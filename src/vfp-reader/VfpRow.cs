using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VfpReader.Internal;

namespace VfpReader
{
    /// <summary>
    /// One record of a table, decoded from the fixed-width record area. A row is a snapshot that
    /// owns its bytes: it stays valid after the reader advances and after the table is disposed.
    /// Values are decoded on demand, so an unused column is never decoded.
    /// </summary>
    public sealed class VfpRow
    {
        private const byte DeletedFlag = (byte)'*';

        /// <summary>
        /// The Julian Day Number of <see cref="DateTime.MinValue"/> under the proleptic Gregorian
        /// calendar. A <c>T</c> / <c>@</c> field stores this number plus milliseconds.
        /// </summary>
        private const long JulianEpoch = 1721426L;

        private readonly VfpHeader _header;
        private readonly Encoding _encoding;
        private readonly byte[] _record;
        private readonly bool _trim;
        private readonly MemoReader? _memo;

        internal VfpRow(
            VfpHeader header,
            Encoding encoding,
            byte[] record,
            long recordNumber,
            bool trim,
            MemoReader? memo = null)
        {
            _header = header;
            _encoding = encoding;
            _record = record;
            _trim = trim;
            _memo = memo;
            RecordNumber = recordNumber;
        }

        /// <summary>
        /// The 1-based number of this record in the file. Deleted records that were skipped still
        /// consume a number, so the numbers are not necessarily consecutive in a filtered stream.
        /// </summary>
        public long RecordNumber { get; }

        /// <summary>Whether the record's deletion flag is <c>*</c>.</summary>
        public bool IsDeleted
        {
            get { return _record.Length > 0 && _record[0] == DeletedFlag; }
        }

        /// <summary>The number of visible columns, the same as <see cref="VfpSchema.FieldCount"/>.</summary>
        public int FieldCount
        {
            get { return _header.Fields.Length; }
        }

        /// <summary>
        /// The decoded value of the field at <paramref name="index"/>: <c>C</c> is a
        /// <see cref="string"/>, <c>N</c> a <see cref="decimal"/>, <c>F</c> / <c>B</c> / <c>O</c>
        /// a <see cref="double"/>, <c>I</c> / <c>+</c> an <see cref="int"/>, <c>Y</c> a
        /// <see cref="decimal"/>, <c>L</c> a <see cref="bool"/>, <c>D</c> / <c>T</c> / <c>@</c> a
        /// <see cref="DateTime"/>, <c>M</c> a <see cref="string"/> read from the memo file,
        /// <c>G</c> / <c>P</c> / <c>W</c> a <c>byte[]</c> read from the memo file, and
        /// <c>Unknown</c> a <c>byte[]</c>. A blank, unparseable or absent value is <c>null</c>.
        /// </summary>
        public object? this[int index]
        {
            get
            {
                if (index < 0 || index >= _header.Fields.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return ReadField(_header.Fields[index]);
            }
        }

        /// <summary>The decoded value of the field called <paramref name="name"/>. Case-insensitive.</summary>
        /// <exception cref="KeyNotFoundException">No field has that name.</exception>
        public object? this[string name]
        {
            get
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                int index = _header.FieldIndex(name);
                if (index < 0)
                {
                    throw new KeyNotFoundException("no field named " + name);
                }

                return ReadField(_header.Fields[index]);
            }
        }

        private object? ReadField(VfpField field)
        {
            switch (field.Type)
            {
                case VfpFieldType.Character:
                    return field.IsBinary ? ReadBytes(field) : ReadCharacter(field);

                case VfpFieldType.Numeric:
                    return ReadNumeric(field);

                case VfpFieldType.Float:
                    return ReadFloat(field);

                case VfpFieldType.Integer:
                case VfpFieldType.AutoIncrement:
                    return ReadInt32(field.Offset);

                case VfpFieldType.Currency:
                    return (decimal)ReadInt64(field.Offset) / 10000m;

                case VfpFieldType.Double:
                case VfpFieldType.DoubleO:
                    return BitConverter.Int64BitsToDouble(ReadInt64(field.Offset));

                case VfpFieldType.Logical:
                    return ReadLogical(_record[field.Offset]);

                case VfpFieldType.Date:
                    return ReadDate(field.Offset);

                case VfpFieldType.DateTime:
                case VfpFieldType.DateTimeAt:
                    return ReadDateTime(field.Offset);

                case VfpFieldType.Memo:
                    return ReadMemo(field, binary: false);

                case VfpFieldType.General:
                case VfpFieldType.Picture:
                case VfpFieldType.Blob:
                    return ReadMemo(field, binary: true);

                case VfpFieldType.Varchar:
                case VfpFieldType.Varbinary:
                    throw new NotSupportedException(
                        "Reading varlength fields (type " + (char)field.Type + ") is not implemented yet.");

                default:
                    return ReadBytes(field);
            }
        }

        private string ReadCharacter(VfpField field)
        {
            string text = _encoding.GetString(_record, field.Offset, field.Width);
            return _trim ? text.TrimEnd(' ', '\0') : text;
        }

        private byte[] ReadBytes(VfpField field)
        {
            var bytes = new byte[field.Width];
            Buffer.BlockCopy(_record, field.Offset, bytes, 0, field.Width);
            return bytes;
        }

        /// <summary>
        /// Resolves a memo value through the table's memo file. A table flagged as having memo
        /// fields but opened without its sibling memo file reads as empty (D13), and a zero
        /// pointer, non-text block or out-of-range length also reads as empty rather than throwing.
        /// Text (<c>M</c>) is decoded with the table code page; general, picture and blob values
        /// (<c>G</c> / <c>P</c> / <c>W</c>) are binary and returned as-is.
        /// </summary>
        private object? ReadMemo(VfpField field, bool binary)
        {
            if (_memo is null)
            {
                return null;
            }

            long block = _memo.Pointer(_record, field.Offset, field.Width);
            byte[]? payload = _memo.Read(block);
            if (payload is null)
            {
                return null;
            }

            return binary ? payload : _encoding.GetString(payload);
        }

        private object? ReadNumeric(VfpField field)
        {
            string text = ReadTrimmedText(field);
            if (text.Length == 0)
            {
                return null;
            }

            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
                ? value
                : (object?)null;
        }

        private object? ReadFloat(VfpField field)
        {
            string text = ReadTrimmedText(field);
            if (text.Length == 0)
            {
                return null;
            }

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : (object?)null;
        }

        /// <summary>The field's text with surrounding whitespace removed.</summary>
        private string ReadTrimmedText(VfpField field)
        {
            return _encoding.GetString(_record, field.Offset, field.Width).Trim();
        }

        private static bool? ReadLogical(byte value)
        {
            switch (value)
            {
                case (byte)'T':
                case (byte)'t':
                case (byte)'Y':
                case (byte)'y':
                    return true;

                case (byte)'F':
                case (byte)'f':
                case (byte)'N':
                case (byte)'n':
                    return false;

                default:
                    return null;
            }
        }

        private DateTime? ReadDate(int offset)
        {
            // D is eight ASCII digits, YYYYMMDD. A blank or impossible date reads as null.
            string text = Encoding.ASCII.GetString(_record, offset, 8).Trim('\0', ' ');
            if (text.Length != 8)
            {
                return null;
            }

            return DateTime.TryParseExact(
                text,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime value)
                ? value
                : (DateTime?)null;
        }

        private DateTime? ReadDateTime(int offset)
        {
            // T / @ are a 4-byte little-endian Julian day number plus milliseconds after midnight.
            int julianDay = ReadInt32(offset);
            int milliseconds = ReadInt32(offset + 4);
            if (julianDay == 0)
            {
                return null;
            }

            long ticks = ((long)julianDay - JulianEpoch) * TimeSpan.TicksPerDay
                + (long)milliseconds * TimeSpan.TicksPerMillisecond;
            if (ticks < 0 || ticks > DateTime.MaxValue.Ticks)
            {
                return null;
            }

            return DateTime.MinValue.AddTicks(ticks);
        }

        private int ReadInt32(int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(_record.AsSpan(offset));
        }

        private long ReadInt64(int offset)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(_record.AsSpan(offset));
        }
    }
}

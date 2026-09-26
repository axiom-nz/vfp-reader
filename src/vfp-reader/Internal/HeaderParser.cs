using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VfpReader.Internal
{
    /// <summary>
    /// Parses the fixed 32-byte header and the field-descriptor array. Ported from FoxDevStudio's
    /// <c>crates/foxvm/src/dbf/layout.rs</c> (MIT).
    /// </summary>
    internal static class HeaderParser
    {
        internal const int HeaderSize = 32;
        private const int FieldDescriptorLength = 32;
        private const byte FieldTerminator = 0x0D;
        private const int DbBacklinkLength = 263;
        private const string NullFlagsFieldName = "_NullFlags";

        internal static VfpHeader Parse(
            byte[] header,
            int headerLength,
            Encoding encoding,
            CodePage codePage,
            string? path)
        {
            if (headerLength < HeaderSize)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "not a DBF file: header is only {0} bytes", headerLength),
                    path,
                    0);
            }

            if (header.Length < headerLength)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "not a DBF file: expected {0} header bytes but got {1}", headerLength, header.Length),
                    path,
                    0);
            }

            byte version = header[0];
            long recordCount = ReadUInt32(header, 4);
            int declaredHeaderLength = ReadUInt16(header, 8);
            int recordLength = ReadUInt16(header, 10);
            bool hasIndex = (header[28] & 0x01) != 0;
            bool hasMemoFlag = (header[28] & 0x02) != 0;
            bool isDatabase = (header[28] & 0x04) != 0;

            if (declaredHeaderLength < HeaderSize + 1)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "header length {0} is too small for any field", declaredHeaderLength),
                    path,
                    8);
            }

            if (declaredHeaderLength != headerLength)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "header length {0} does not match the {1} bytes read", declaredHeaderLength, headerLength),
                    path,
                    8);
            }

            if (recordLength < 1)
            {
                throw new VfpFormatException("record length is 0", path, 10);
            }

            DateTime? lastUpdate = ParseLastUpdate(header);

            List<RawField> raw = ParseFieldDescriptors(header, headerLength, encoding, path);
            if (raw.Count == 0)
            {
                throw new VfpFormatException("the table has no fields", path, HeaderSize);
            }

            if (!TryLayout(raw, recordLength, out int[] offsets, out int[] widths))
            {
                int needed = 1;
                foreach (RawField field in raw)
                {
                    needed += field.Length;
                }

                throw new VfpFormatException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "record length {0} is too small for the {1} fields, which need {2} bytes",
                        recordLength,
                        raw.Count,
                        needed),
                    path,
                    10);
            }

            int hidden = -1;
            for (int i = 0; i < raw.Count; i++)
            {
                if (string.Equals(raw[i].Name, NullFlagsFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    hidden = i;
                    break;
                }
            }

            int nullFlagsOffset = hidden >= 0 ? offsets[hidden] : 0;
            int nullFlagsWidth = hidden >= 0 ? widths[hidden] : 0;

            var fields = new List<VfpField>(raw.Count);
            for (int i = 0; i < raw.Count; i++)
            {
                if (i == hidden)
                {
                    continue;
                }

                VfpField field = raw[i].ToField(offsets[i], widths[i]);
                fields.Add(field);
            }

            if (fields.Count == 0)
            {
                throw new VfpFormatException("the table has no fields", path, HeaderSize);
            }

            AssignNullBits(fields);

            string databasePath = string.Empty;
            if (VfpHeader.IsVisualFoxPro(version))
            {
                databasePath = ReadDatabaseBacklink(header, headerLength, encoding);
            }

            bool hasMemo = hasMemoFlag || VfpHeader.VersionHasMemo(version) || VfpHeader.FieldsHaveMemo(fields.ToArray());

            return new VfpHeader(
                version,
                fields.ToArray(),
                recordCount,
                headerLength,
                recordLength,
                codePage,
                hasIndex,
                hasMemo,
                isDatabase,
                lastUpdate,
                databasePath,
                nullFlagsOffset,
                nullFlagsWidth);
        }

        private static DateTime? ParseLastUpdate(byte[] header)
        {
            int year = 1900 + header[1];
            int month = header[2];
            int day = header[3];
            if (month < 1 || month > 12 || day < 1 || day > 31 || year > 9999)
            {
                return null;
            }

            if (day > DateTime.DaysInMonth(year, month))
            {
                return null;
            }

            return new DateTime(year, month, day);
        }

        private static List<RawField> ParseFieldDescriptors(byte[] header, int headerLength, Encoding encoding, string? path)
        {
            var fields = new List<RawField>();
            int position = HeaderSize;
            bool terminated = false;

            while (position < headerLength)
            {
                if (header[position] == FieldTerminator)
                {
                    terminated = true;
                    break;
                }

                int end = position + FieldDescriptorLength;
                if (end > headerLength)
                {
                    throw new VfpFormatException(
                        "a field descriptor runs past the end of the header",
                        path,
                        position);
                }

                fields.Add(ParseFieldDescriptor(header, position, encoding));
                position = end;
            }

            if (!terminated)
            {
                throw new VfpFormatException(
                    "the field descriptors are not terminated by 0x0D",
                    path,
                    position);
            }

            return fields;
        }

        private static RawField ParseFieldDescriptor(byte[] header, int position, Encoding encoding)
        {
            int nameLength = 0;
            while (nameLength < 11 && header[position + nameLength] != 0)
            {
                nameLength++;
            }

            string name = encoding.GetString(header, position, nameLength).Trim();
            VfpFieldType type = VfpFieldTypes.FromByte(header[position + 11]);
            byte length = header[position + 16];
            byte decimals = header[position + 17];
            byte flags = header[position + 18];
            bool nullable = (flags & 0x02) != 0;
            bool isSystem = (flags & 0x01) != 0;
            bool isBinary = (flags & 0x04) != 0;
            bool autoIncrement = (flags & 0x0C) == 0x0C;
            uint autoIncrementNext = autoIncrement ? ReadUInt32(header, position + 19) : 0;
            byte autoIncrementStep = autoIncrement ? Math.Max((byte)1, header[position + 23]) : (byte)0;

            return new RawField(
                name,
                type,
                length,
                decimals,
                nullable,
                isSystem,
                isBinary,
                autoIncrement,
                autoIncrementNext,
                autoIncrementStep);
        }

        private static bool TryLayout(
            List<RawField> fields,
            int recordLength,
            out int[] offsets,
            out int[] widths)
        {
            bool[] conventions = { true, false };
            foreach (bool wideChar in conventions)
            {
                int position = 1;
                var candidateOffsets = new int[fields.Count];
                var candidateWidths = new int[fields.Count];

                for (int i = 0; i < fields.Count; i++)
                {
                    int width = fields[i].WidthForLayout(wideChar);
                    candidateOffsets[i] = position;
                    candidateWidths[i] = width;
                    position += width;
                }

                if (position <= recordLength)
                {
                    offsets = candidateOffsets;
                    widths = candidateWidths;
                    return true;
                }

                bool hasWideCandidate = false;
                foreach (RawField field in fields)
                {
                    if (field.Type == VfpFieldType.Character && field.Decimals > 0)
                    {
                        hasWideCandidate = true;
                        break;
                    }
                }

                if (!hasWideCandidate)
                {
                    break;
                }
            }

            offsets = Array.Empty<int>();
            widths = Array.Empty<int>();
            return false;
        }

        private static void AssignNullBits(List<VfpField> fields)
        {
            int bit = 0;
            foreach (VfpField field in fields)
            {
                if (field.IsNullable)
                {
                    field.NullBit = bit;
                    bit++;
                }
            }
        }

        private static string ReadDatabaseBacklink(byte[] header, int headerLength, Encoding encoding)
        {
            int start = HeaderSize;
            int terminator = -1;
            while (start < headerLength)
            {
                if (header[start] == FieldTerminator)
                {
                    terminator = start;
                    break;
                }

                start += FieldDescriptorLength;
            }

            if (terminator < 0)
            {
                return string.Empty;
            }

            int backlinkStart = terminator + 1;
            int available = headerLength - backlinkStart;
            if (available <= 0)
            {
                return string.Empty;
            }

            int length = Math.Min(DbBacklinkLength, available);
            string path = encoding.GetString(header, backlinkStart, length);
            return path.TrimEnd('\0').Trim();
        }

        private static ushort ReadUInt16(byte[] buffer, int offset)
        {
            return (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset]
                | (buffer[offset + 1] << 8)
                | (buffer[offset + 2] << 16)
                | (buffer[offset + 3] << 24));
        }

        private readonly struct RawField
        {
            internal RawField(
                string name,
                VfpFieldType type,
                byte length,
                byte decimals,
                bool nullable,
                bool isSystem,
                bool isBinary,
                bool autoIncrement,
                uint autoIncrementNext,
                byte autoIncrementStep)
            {
                Name = name;
                Type = type;
                Length = length;
                Decimals = decimals;
                Nullable = nullable;
                IsSystem = isSystem;
                IsBinary = isBinary;
                AutoIncrement = autoIncrement;
                AutoIncrementNext = autoIncrementNext;
                AutoIncrementStep = autoIncrementStep;
            }

            internal string Name { get; }

            internal VfpFieldType Type { get; }

            internal byte Length { get; }

            internal byte Decimals { get; }

            internal bool Nullable { get; }

            internal bool IsSystem { get; }

            internal bool IsBinary { get; }

            internal bool AutoIncrement { get; }

            internal uint AutoIncrementNext { get; }

            internal byte AutoIncrementStep { get; }

            internal int WidthForLayout(bool wideChar)
            {
                return wideChar && Type == VfpFieldType.Character && Decimals > 0
                    ? Length + Decimals * 256
                    : Length;
            }

            internal VfpField ToField(int offset, int width)
            {
                return new VfpField(
                    Name,
                    Type,
                    Length,
                    Decimals,
                    Nullable,
                    IsSystem,
                    IsBinary,
                    AutoIncrement,
                    AutoIncrementNext,
                    AutoIncrementStep)
                {
                    Offset = offset,
                    Width = width,
                };
            }
        }
    }
}

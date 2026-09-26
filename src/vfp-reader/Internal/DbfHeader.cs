using System;
using System.Linq;

namespace VfpReader.Internal
{
    /// <summary>
    /// Everything the header says, plus where each visible field sits inside a record and where
    /// the hidden null-flags field sits. This is the internal, fully-resolved view that
    /// <see cref="DbfSchema"/> and the record decoder share.
    /// </summary>
    internal sealed class DbfHeader
    {
        internal DbfHeader(
            byte version,
            DbfField[] fields,
            long recordCount,
            int headerLength,
            int recordLength,
            CodePage codePage,
            bool hasIndex,
            bool hasMemo,
            bool isDatabase,
            DateTime? lastUpdate,
            string databasePath,
            int nullFlagsOffset,
            int nullFlagsWidth)
        {
            Version = version;
            Fields = fields;
            RecordCount = recordCount;
            HeaderLength = headerLength;
            RecordLength = recordLength;
            CodePage = codePage;
            HasIndex = hasIndex;
            HasMemo = hasMemo;
            IsDatabase = isDatabase;
            LastUpdate = lastUpdate;
            DatabasePath = databasePath;
            NullFlagsOffset = nullFlagsOffset;
            NullFlagsWidth = nullFlagsWidth;
        }

        internal byte Version { get; }

        internal DbfField[] Fields { get; }

        internal long RecordCount { get; }

        internal int HeaderLength { get; }

        internal int RecordLength { get; }

        internal CodePage CodePage { get; }

        internal bool HasIndex { get; }

        internal bool HasMemo { get; }

        internal bool IsDatabase { get; }

        internal DateTime? LastUpdate { get; }

        internal string DatabasePath { get; }

        internal bool HasNullFlags
        {
            get { return NullFlagsWidth > 0; }
        }

        internal int NullFlagsOffset { get; }

        internal int NullFlagsWidth { get; }

        /// <summary>Byte offset of a 1-based record number from the start of the file.</summary>
        internal long RecordOffset(long recordNumber)
        {
            return HeaderLength + (recordNumber - 1) * (long)RecordLength;
        }

        /// <summary>The hidden field, when present. It is not part of <see cref="Fields"/>.</summary>
        internal bool TryGetNullSlot(int index, out int offset, out byte mask)
        {
            offset = 0;
            mask = 0;
            if (!HasNullFlags || index < 0 || index >= Fields.Length)
            {
                return false;
            }

            int? bit = Fields[index].NullBit;
            if (bit is null)
            {
                return false;
            }

            offset = NullFlagsOffset + (bit.Value / 8);
            if (offset >= NullFlagsOffset + NullFlagsWidth)
            {
                return false;
            }

            mask = (byte)(1 << (bit.Value % 8));
            return true;
        }

        /// <summary>Whether the record's null-flags bytes mark <paramref name="index"/> as null.</summary>
        internal bool IsNull(byte[] record, int index)
        {
            if (!TryGetNullSlot(index, out int offset, out byte mask) || offset >= record.Length)
            {
                return false;
            }

            return (record[offset] & mask) != 0;
        }

        /// <summary>The index of a field by name, or -1. Case-insensitive.</summary>
        internal int FieldIndex(string name)
        {
            return Array.FindIndex(
                Fields,
                f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>True when the version byte says a memo file is required.</summary>
        internal static bool VersionHasMemo(byte version)
        {
            return version == 0x83 || version == 0x8B || version == 0xF5 || version == 0xFB;
        }

        /// <summary>True for the Visual FoxPro versions, which carry a database backlink.</summary>
        internal static bool IsVisualFoxPro(byte version)
        {
            return version == 0x30 || version == 0x31 || version == 0x32;
        }

        /// <summary>True when any field is held in the memo file.</summary>
        internal static bool FieldsHaveMemo(DbfField[] fields)
        {
            return fields.Any(
                f => f.Type == DbfFieldType.Memo
                    || f.Type == DbfFieldType.General
                    || f.Type == DbfFieldType.Picture
                    || f.Type == DbfFieldType.Blob);
        }
    }
}

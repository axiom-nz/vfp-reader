using System;
using System.Collections.Generic;

namespace VfpReader
{
    /// <summary>
    /// What a table header says about the table: its version, size, code page and columns. It is
    /// known without reading any records, so it is available as soon as the table is opened.
    /// </summary>
    public sealed class DbfSchema
    {
        private readonly DbfField[] _fields;

        internal DbfSchema(
            byte version,
            long recordCount,
            int headerLength,
            int recordLength,
            DateTime? lastUpdate,
            int codePage,
            string databasePath,
            bool hasIndex,
            bool hasMemo,
            bool isDatabase,
            DbfField[] fields)
        {
            Version = version;
            RecordCount = recordCount;
            HeaderLength = headerLength;
            RecordLength = recordLength;
            LastUpdate = lastUpdate;
            CodePage = codePage;
            DatabasePath = databasePath;
            HasIndex = hasIndex;
            HasMemo = hasMemo;
            IsDatabase = isDatabase;
            _fields = fields;
        }

        /// <summary>The version byte at header offset 0.</summary>
        public byte Version { get; }

        /// <summary>The record count claimed by the header.</summary>
        public long RecordCount { get; }

        /// <summary>The number of bytes before the first record.</summary>
        public int HeaderLength { get; }

        /// <summary>The number of bytes per record, including the deletion flag.</summary>
        public int RecordLength { get; }

        /// <summary>The last-update date from bytes 1 to 3, or <c>null</c> when it is not a date.</summary>
        public DateTime? LastUpdate { get; }

        /// <summary>The effective code page (the header's language driver, or a caller override).</summary>
        public int CodePage { get; }

        /// <summary>
        /// The path of the owning database (a <c>.dbc</c>) from the Visual FoxPro backlink; empty
        /// for a free table.
        /// </summary>
        public string DatabasePath { get; }

        /// <summary>Whether a compound index sits beside the table.</summary>
        public bool HasIndex { get; }

        /// <summary>Whether the table requires a memo file.</summary>
        public bool HasMemo { get; }

        /// <summary>Whether the table is part of a database container.</summary>
        public bool IsDatabase { get; }

        /// <summary>The columns, in file order. The hidden <c>_NullFlags</c> field is never listed.</summary>
        public IReadOnlyList<DbfField> Fields
        {
            get { return _fields; }
        }

        /// <summary>The number of visible columns.</summary>
        public int FieldCount
        {
            get { return _fields.Length; }
        }

        /// <summary>Index of the field called <paramref name="name"/>, or -1. Case-insensitive.</summary>
        public int FieldIndex(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            for (int i = 0; i < _fields.Length; i++)
            {
                if (string.Equals(_fields[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The field called <paramref name="name"/>, or <c>null</c>. Case-insensitive.</summary>
        public DbfField? FindField(string name)
        {
            int index = FieldIndex(name);
            return index < 0 ? null : _fields[index];
        }
    }
}

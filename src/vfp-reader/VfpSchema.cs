using System;
using System.Collections.Generic;
using VfpReader.Internal;

namespace VfpReader
{
    /// <summary>
    /// What a table header says about the table: its version, size, code page and columns. It is
    /// known without reading any records, so it is available as soon as the table is opened.
    /// </summary>
    public sealed class VfpSchema
    {
        private readonly VfpHeader _header;

        internal VfpSchema(VfpHeader header)
        {
            _header = header;
        }

        /// <summary>The version byte at header offset 0.</summary>
        public byte Version
        {
            get { return _header.Version; }
        }

        /// <summary>The record count claimed by the header.</summary>
        public long RecordCount
        {
            get { return _header.RecordCount; }
        }

        /// <summary>The number of bytes before the first record.</summary>
        public int HeaderLength
        {
            get { return _header.HeaderLength; }
        }

        /// <summary>The number of bytes per record, including the deletion flag.</summary>
        public int RecordLength
        {
            get { return _header.RecordLength; }
        }

        /// <summary>The last-update date from bytes 1 to 3, or <c>null</c> when it is not a date.</summary>
        public DateTime? LastUpdate
        {
            get { return _header.LastUpdate; }
        }

        /// <summary>
        /// The effective code page: the header's language driver, or the caller's
        /// <see cref="VfpReadOptions.Encoding"/> override.
        /// </summary>
        public CodePage CodePage
        {
            get { return _header.CodePage; }
        }

        /// <summary>
        /// The path of the owning database (a <c>.dbc</c>) from the Visual FoxPro backlink; empty
        /// for a free table.
        /// </summary>
        public string DatabasePath
        {
            get { return _header.DatabasePath; }
        }

        /// <summary>Whether a compound index sits beside the table.</summary>
        public bool HasIndex
        {
            get { return _header.HasIndex; }
        }

        /// <summary>Whether the table requires a memo file.</summary>
        public bool HasMemo
        {
            get { return _header.HasMemo; }
        }

        /// <summary>Whether the table is part of a database container.</summary>
        public bool IsDatabase
        {
            get { return _header.IsDatabase; }
        }

        /// <summary>The columns, in file order. The hidden <c>_NullFlags</c> field is never listed.</summary>
        public IReadOnlyList<VfpField> Fields
        {
            get { return _header.Fields; }
        }

        /// <summary>The number of visible columns.</summary>
        public int FieldCount
        {
            get { return _header.Fields.Length; }
        }

        /// <summary>Index of the field called <paramref name="name"/>, or -1. Case-insensitive.</summary>
        public int FieldIndex(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return _header.FieldIndex(name);
        }

        /// <summary>The field called <paramref name="name"/>, or <c>null</c>. Case-insensitive.</summary>
        public VfpField? FindField(string name)
        {
            int index = FieldIndex(name);
            return index < 0 ? null : _header.Fields[index];
        }
    }
}

using System.Globalization;

namespace VfpReader
{
    /// <summary>
    /// One column of a table, in file order.
    /// </summary>
    public sealed class DbfField
    {
        internal DbfField(
            string name,
            DbfFieldType type,
            byte length,
            byte decimals,
            bool isNullable,
            bool isSystem,
            bool isBinary,
            bool isAutoIncrement,
            uint autoIncrementNext,
            byte autoIncrementStep)
        {
            Name = name;
            Type = type;
            Length = length;
            Decimals = decimals;
            IsNullable = isNullable;
            IsSystem = isSystem;
            IsBinary = isBinary;
            IsAutoIncrement = isAutoIncrement;
            AutoIncrementNext = autoIncrementNext;
            AutoIncrementStep = autoIncrementStep;
        }

        /// <summary>The field name, decoded with the table code page and trimmed.</summary>
        public string Name { get; }

        /// <summary>The type parsed from the descriptor's type character.</summary>
        public DbfFieldType Type { get; }

        /// <summary>The raw length byte from the descriptor.</summary>
        public byte Length { get; }

        /// <summary>The raw decimals byte from the descriptor.</summary>
        public byte Decimals { get; }

        /// <summary>Whether the field accepts the empty value (<c>NULL</c>).</summary>
        public bool IsNullable { get; }

        /// <summary>Whether the field is the table's own rather than the program's.</summary>
        public bool IsSystem { get; }

        /// <summary>Whether the field holds raw bytes rather than code-page text.</summary>
        public bool IsBinary { get; }

        /// <summary>Whether the field fills itself in as records are added.</summary>
        public bool IsAutoIncrement { get; }

        /// <summary>The value the next inserted record receives, when autoincrementing.</summary>
        public uint AutoIncrementNext { get; }

        /// <summary>The amount the field increases by, when autoincrementing; otherwise zero.</summary>
        public byte AutoIncrementStep { get; }

        /// <summary>Byte offset of the field inside a record, including the deletion flag.</summary>
        internal int Offset { get; set; }

        /// <summary>Width of the field inside a record.</summary>
        internal int Width { get; set; }

        /// <summary>
        /// Index of this field's bit inside the hidden <c>_NullFlags</c> field, when the field
        /// accepts nulls. See the layout notes: the bit order for <c>V</c> / <c>Q</c> fields is
        /// still being verified against a Visual FoxPro 9 table.
        /// </summary>
        internal int? NullBit { get; set; }

        /// <summary>Width the field occupies for a given character-widening convention.</summary>
        internal int WidthForLayout(bool wideChar)
        {
            return wideChar && Type == DbfFieldType.Character && Decimals > 0
                ? Length + Decimals * 256
                : Length;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}({2},{3})",
                Name,
                (char)Type,
                Length,
                Decimals);
        }
    }
}

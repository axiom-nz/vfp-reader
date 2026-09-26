using System.Globalization;

namespace VfpReader
{
    /// <summary>
    /// One column of a table, in file order.
    /// </summary>
    public sealed class VfpField
    {
        internal VfpField(
            string name,
            VfpFieldType type,
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
        public VfpFieldType Type { get; }

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
        /// Index of this field's null bit inside the hidden <c>_NullFlags</c> field, when the
        /// field accepts nulls. Bits are allocated in field order; for a <c>V</c> / <c>Q</c>
        /// field the null bit follows that field's <see cref="VarlengthBit"/>.
        /// </summary>
        internal int? NullBit { get; set; }

        /// <summary>
        /// Index of this field's "varlength" bit inside the hidden <c>_NullFlags</c> field, for a
        /// <c>V</c> / <c>Q</c> field. When the bit is set the value's length is the last byte of
        /// the field; otherwise the value fills the whole field.
        /// </summary>
        internal int? VarlengthBit { get; set; }

        /// <summary>Whether this is a varlength field (<c>V</c> or <c>Q</c>).</summary>
        internal bool IsVarlength
        {
            get { return Type == VfpFieldType.Varchar || Type == VfpFieldType.Varbinary; }
        }

        /// <summary>Width the field occupies for a given character-widening convention.</summary>
        internal int WidthForLayout(bool wideChar)
        {
            return WidthFor(Type, Length, Decimals, wideChar);
        }

        /// <summary>
        /// The record width of a descriptor under one character-widening convention. FoxPro 2.x
        /// sometimes stores a wide character field as <c>length + decimals * 256</c>; the layout
        /// tries the widened reading first and falls back to the raw length.
        /// </summary>
        internal static int WidthFor(VfpFieldType type, int length, int decimals, bool wideChar)
        {
            return wideChar && type == VfpFieldType.Character && decimals > 0
                ? length + decimals * 256
                : length;
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

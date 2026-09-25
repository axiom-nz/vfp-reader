namespace VfpReader
{
    /// <summary>
    /// The one-byte type code stored in a field descriptor at offset 11. The numeric value of each
    /// member is the ASCII character that appears in the file.
    /// </summary>
    public enum DbfFieldType : byte
    {
        /// <summary>A type character this reader does not know; the raw field bytes are returned.</summary>
        Unknown = 0,

        /// <summary>Character text, padded with spaces.</summary>
        Character = (byte)'C',

        /// <summary>Numeric, stored right-aligned as ASCII.</summary>
        Numeric = (byte)'N',

        /// <summary>Floating point, stored right-aligned as ASCII.</summary>
        Float = (byte)'F',

        /// <summary>Logical: T/F/Y/N, or '?' for unknown.</summary>
        Logical = (byte)'L',

        /// <summary>Date, eight ASCII digits <c>YYYYMMDD</c>.</summary>
        Date = (byte)'D',

        /// <summary>DateTime: a four-byte Julian day plus milliseconds after midnight.</summary>
        DateTime = (byte)'T',

        /// <summary>Memo text, held in the sibling <c>.fpt</c> or <c>.dbt</c> file.</summary>
        Memo = (byte)'M',

        /// <summary>General (OLE) binary, held in the memo file.</summary>
        General = (byte)'G',

        /// <summary>Picture binary, held in the memo file.</summary>
        Picture = (byte)'P',

        /// <summary>Currency: an eight-byte little-endian integer of ten-thousandths.</summary>
        Currency = (byte)'Y',

        /// <summary>Four-byte little-endian integer.</summary>
        Integer = (byte)'I',

        /// <summary>Eight-byte little-endian IEEE double, as Visual FoxPro writes it.</summary>
        Double = (byte)'B',

        /// <summary>Eight-byte little-endian IEEE double, as dBASE writes it.</summary>
        DoubleO = (byte)'O',

        /// <summary>Four-byte little-endian autoincrementing integer.</summary>
        AutoIncrement = (byte)'+',

        /// <summary>Variable-length character (Visual FoxPro 9).</summary>
        Varchar = (byte)'V',

        /// <summary>Variable-length binary (Visual FoxPro 9).</summary>
        Varbinary = (byte)'Q',

        /// <summary>Blob, held in the memo file.</summary>
        Blob = (byte)'W',

        /// <summary>DateTime, the FoxPro 2.x spelling of <see cref="DateTime"/>.</summary>
        DateTimeAt = (byte)'@',

        /// <summary>The hidden null-flags field; it is never listed in a schema.</summary>
        NullFlags = (byte)'0',
    }

    internal static class DbfFieldTypes
    {
        /// <summary>Maps a raw type byte to a known member, or <see cref="DbfFieldType.Unknown"/>.</summary>
        internal static DbfFieldType FromByte(byte code)
        {
            switch (code)
            {
                case (byte)'C': return DbfFieldType.Character;
                case (byte)'N': return DbfFieldType.Numeric;
                case (byte)'F': return DbfFieldType.Float;
                case (byte)'L': return DbfFieldType.Logical;
                case (byte)'D': return DbfFieldType.Date;
                case (byte)'T': return DbfFieldType.DateTime;
                case (byte)'M': return DbfFieldType.Memo;
                case (byte)'G': return DbfFieldType.General;
                case (byte)'P': return DbfFieldType.Picture;
                case (byte)'Y': return DbfFieldType.Currency;
                case (byte)'I': return DbfFieldType.Integer;
                case (byte)'B': return DbfFieldType.Double;
                case (byte)'O': return DbfFieldType.DoubleO;
                case (byte)'+': return DbfFieldType.AutoIncrement;
                case (byte)'V': return DbfFieldType.Varchar;
                case (byte)'Q': return DbfFieldType.Varbinary;
                case (byte)'W': return DbfFieldType.Blob;
                case (byte)'@': return DbfFieldType.DateTimeAt;
                case (byte)'0': return DbfFieldType.NullFlags;
                default: return DbfFieldType.Unknown;
            }
        }
    }
}

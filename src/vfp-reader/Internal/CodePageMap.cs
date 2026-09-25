namespace VfpReader.Internal
{
    /// <summary>
    /// Maps the DBF header language-driver byte (offset 29) to a code page number. Ported from
    /// FoxDevStudio's <c>crates/foxvm/src/dbf/encoding.rs</c> (MIT).
    /// </summary>
    internal static class CodePageMap
    {
        /// <summary>The code page assumed when the header records none.</summary>
        internal const int DefaultCodePage = 1252;

        /// <summary>
        /// The code page for a language driver, or <c>null</c> when the byte is not one this
        /// reader knows. Callers fall back to <see cref="DefaultCodePage"/>.
        /// </summary>
        internal static int? FromLanguageDriver(byte id)
        {
            switch (id)
            {
                case 0x01:
                case 0x09:
                case 0x0B:
                case 0x0D:
                case 0x0F:
                case 0x11:
                case 0x15:
                case 0x18:
                case 0x19:
                case 0x1B:
                    return 437;

                case 0x02:
                case 0x0A:
                case 0x0E:
                case 0x10:
                case 0x12:
                case 0x14:
                case 0x16:
                case 0x1A:
                case 0x1D:
                case 0x25:
                case 0x37:
                    return 850;

                case 0x03:
                case 0x57:
                case 0x58:
                case 0x59:
                    return 1252;

                case 0x04:
                case 0x98:
                    return 10000;

                case 0x08:
                case 0x17:
                case 0x66:
                    return 865;

                case 0x13:
                case 0x7B:
                    return 932;

                case 0x1C:
                case 0x6C:
                    return 863;

                case 0x1F:
                case 0x22:
                case 0x23:
                case 0x40:
                case 0x64:
                    return 852;

                case 0x24:
                    return 860;

                case 0x26:
                case 0x65:
                    return 866;

                case 0x4D:
                case 0x7A:
                    return 936;

                case 0x4E:
                case 0x79:
                    return 949;

                case 0x4F:
                case 0x78:
                    return 950;

                case 0x50:
                case 0x7C:
                    return 874;

                case 0x67:
                    return 861;

                case 0x6A:
                    return 737;

                case 0x6B:
                    return 857;

                case 0x7D:
                    return 1255;

                case 0x7E:
                    return 1256;

                case 0x96:
                    return 10007;

                case 0x97:
                    return 10029;

                case 0xC8:
                    return 1250;

                case 0xC9:
                    return 1251;

                case 0xCA:
                    return 1254;

                case 0xCB:
                    return 1253;

                case 0xCC:
                    return 1257;

                default:
                    return null;
            }
        }
    }
}

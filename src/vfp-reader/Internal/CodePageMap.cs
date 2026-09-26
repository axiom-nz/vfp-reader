namespace VfpReader.Internal
{
    /// <summary>
    /// Maps the DBF language-driver byte (header offset 29) to a code page. Ported from
    /// FoxDevStudio's <c>crates/foxvm/src/dbf/encoding.rs</c> (MIT).
    /// </summary>
    /// <remarks>
    /// The driver records the language a table was written in, not the page its bytes are in, so
    /// several drivers share one page. This class is only the lookup: <see cref="DbfTable"/>
    /// resolves the number through <c>CodePagesEncodingProvider</c>, and
    /// <see cref="DbfReadOptions.Encoding"/> can bypass it entirely.
    ///
    /// Unknown drivers, including <c>0x00</c> ("no code page recorded"), return <c>null</c> and the
    /// caller falls back to <see cref="DefaultCodePage"/>. The FoxDevStudio reader decodes only
    /// 437, 850 and 1252 itself and folds every other page into that same default; this port reports
    /// the page for every driver it knows and lets .NET decode it.
    /// </remarks>
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
                // IBM437 - DOS United States
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

                // IBM850 - DOS Latin-1
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

                // Windows-1252 - the VFP default
                case 0x03:
                case 0x57:
                case 0x58:
                case 0x59:
                    return 1252;

                // MacRoman
                case 0x04:
                case 0x98:
                    return 10000;

                // IBM865 - DOS Nordic
                case 0x08:
                case 0x17:
                case 0x66:
                    return 865;

                // Shift-JIS - Japanese
                case 0x13:
                case 0x7B:
                    return 932;

                // IBM863 - DOS Canadian French
                case 0x1C:
                case 0x6C:
                    return 863;

                // IBM852 - DOS Latin-2
                case 0x1F:
                case 0x22:
                case 0x23:
                case 0x40:
                case 0x64:
                    return 852;

                // IBM860 - DOS Portuguese
                case 0x24:
                    return 860;

                // IBM866 - DOS Cyrillic (Russian)
                case 0x26:
                case 0x65:
                    return 866;

                // GBK - Simplified Chinese
                case 0x4D:
                case 0x7A:
                    return 936;

                // Korean (EUC-KR)
                case 0x4E:
                case 0x79:
                    return 949;

                // Big5 - Traditional Chinese
                case 0x4F:
                case 0x78:
                    return 950;

                // Windows-874 - Thai
                case 0x50:
                case 0x7C:
                    return 874;

                // IBM861 - DOS Icelandic
                case 0x67:
                    return 861;

                // IBM737 - DOS Greek
                case 0x6A:
                    return 737;

                // IBM857 - DOS Turkish
                case 0x6B:
                    return 857;

                // Windows-1255 - Hebrew
                case 0x7D:
                    return 1255;

                // Windows-1256 - Arabic
                case 0x7E:
                    return 1256;

                // MacCyrillic
                case 0x96:
                    return 10007;

                // MacCentralEurope
                case 0x97:
                    return 10029;

                // Windows-1250 - Central European
                case 0xC8:
                    return 1250;

                // Windows-1251 - Cyrillic
                case 0xC9:
                    return 1251;

                // Windows-1254 - Turkish
                case 0xCA:
                    return 1254;

                // Windows-1253 - Greek
                case 0xCB:
                    return 1253;

                // Windows-1257 - Baltic
                case 0xCC:
                    return 1257;

                default:
                    return null;
            }
        }
    }
}

namespace VfpReader.Internal
{
    /// <summary>
    /// Maps the DBF language-driver byte (header offset 29) to a <see cref="CodePage"/>.
    /// </summary>
    /// <remarks>
    /// The driver records the language a table was written in, not the page its bytes are in, so
    /// several drivers share one page. The mapping is the dBase / FoxPro language-driver ID (LDID)
    /// table; the code page numbers are the Windows identifiers
    /// <see cref="System.Text.Encoding.GetEncoding(int)"/> accepts. The .NET base class library has
    /// no notion of the language-driver byte, so the table lives here. <see cref="DbfTable"/>
    /// resolves the page through <c>CodePagesEncodingProvider</c>, and
    /// <see cref="DbfReadOptions.Encoding"/> can bypass the lookup entirely.
    ///
    /// Unknown drivers, including <c>0x00</c> ("no code page recorded"), return <c>null</c> and the
    /// caller falls back to <see cref="DefaultCodePage"/>. The FoxDevStudio reader (which this was
    /// ported from) decodes only 437, 850 and 1252 itself and folds every other page into that same
    /// default; this port reports the page for every driver it knows and lets .NET decode it.
    /// </remarks>
    internal static class CodePageMap
    {
        /// <summary>The code page assumed when the header records none.</summary>
        internal const CodePage DefaultCodePage = CodePage.Windows1252;

        /// <summary>
        /// The code page for a language driver, or <c>null</c> when the byte is not one this
        /// reader knows. Callers fall back to <see cref="DefaultCodePage"/>.
        /// </summary>
        internal static CodePage? FromLanguageDriver(byte id)
        {
            switch ((LanguageDriver)id)
            {
                case LanguageDriver.DosUnitedStates:
                case LanguageDriver.DosDutch:
                case LanguageDriver.DosFinnish:
                case LanguageDriver.DosFrench:
                case LanguageDriver.DosGerman:
                case LanguageDriver.DosItalian:
                case LanguageDriver.DosSwedish:
                case LanguageDriver.DosSpanish:
                case LanguageDriver.DosEnglishBritain:
                case LanguageDriver.DosEnglishUnitedStates:
                    return CodePage.Ibm437;

                case LanguageDriver.DosInternational:
                case LanguageDriver.DosDutchSecondary:
                case LanguageDriver.DosFrenchSecondary:
                case LanguageDriver.DosGermanSecondary:
                case LanguageDriver.DosItalianSecondary:
                case LanguageDriver.DosSpanishSecondary:
                case LanguageDriver.DosSwedishSecondary:
                case LanguageDriver.DosEnglishBritainSecondary:
                case LanguageDriver.DosFrenchCanadianSecondary:
                case LanguageDriver.DosPortugueseSecondary:
                case LanguageDriver.DosEnglishUnitedStatesSecondary:
                    return CodePage.Ibm850;

                case LanguageDriver.WindowsAnsi:
                case LanguageDriver.Ansi:
                case LanguageDriver.WesternEuropeanAnsi:
                case LanguageDriver.SpanishAnsi:
                    return CodePage.Windows1252;

                case LanguageDriver.MacStandard:
                    return CodePage.MacRoman;

                case LanguageDriver.MacGreek:
                    return CodePage.MacGreek;

                case LanguageDriver.DosDanish:
                case LanguageDriver.DosNorwegian:
                case LanguageDriver.DosNordic:
                    return CodePage.Ibm865;

                case LanguageDriver.JapaneseShiftJis:
                case LanguageDriver.JapaneseWindows:
                    return CodePage.ShiftJis;

                case LanguageDriver.DosFrenchCanadian:
                case LanguageDriver.DosFrenchCanadianAlternate:
                    return CodePage.Ibm863;

                case LanguageDriver.DosCzech:
                case LanguageDriver.DosHungarian:
                case LanguageDriver.DosPolish:
                case LanguageDriver.DosRomanian:
                case LanguageDriver.DosEasternEuropean:
                    return CodePage.Ibm852;

                case LanguageDriver.DosPortuguese:
                    return CodePage.Ibm860;

                case LanguageDriver.DosRussian:
                case LanguageDriver.DosRussianSecondary:
                    return CodePage.Ibm866;

                case LanguageDriver.ChineseGbk:
                case LanguageDriver.ChineseSimplifiedWindows:
                    return CodePage.Gbk;

                case LanguageDriver.KoreanAnsiOem:
                case LanguageDriver.KoreanWindows:
                    return CodePage.EucKr;

                case LanguageDriver.ChineseBig5:
                case LanguageDriver.ChineseTraditionalWindows:
                    return CodePage.Big5;

                case LanguageDriver.ThaiAnsiOem:
                case LanguageDriver.ThaiWindows:
                    return CodePage.Windows874;

                case LanguageDriver.DosIcelandic:
                    return CodePage.Ibm861;

                case LanguageDriver.DosGreek:
                    return CodePage.Ibm737;

                case LanguageDriver.DosTurkish:
                    return CodePage.Ibm857;

                case LanguageDriver.HebrewWindows:
                    return CodePage.Windows1255;

                case LanguageDriver.ArabicWindows:
                    return CodePage.Windows1256;

                case LanguageDriver.MacRussian:
                    return CodePage.MacCyrillic;

                case LanguageDriver.MacEasternEuropean:
                    return CodePage.MacCentralEurope;

                case LanguageDriver.WindowsEasternEuropean:
                    return CodePage.Windows1250;

                case LanguageDriver.WindowsRussian:
                    return CodePage.Windows1251;

                case LanguageDriver.WindowsTurkish:
                    return CodePage.Windows1254;

                case LanguageDriver.WindowsGreek:
                    return CodePage.Windows1253;

                case LanguageDriver.WindowsBaltic:
                    return CodePage.Windows1257;

                default:
                    return null;
            }
        }
    }
}

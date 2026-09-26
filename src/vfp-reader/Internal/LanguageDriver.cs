namespace VfpReader.Internal
{
    /// <summary>
    /// The DBF language-driver ID from header offset 29, named after the language or dialect it
    /// records. <see cref="CodePageMap"/> turns it into a <see cref="CodePage"/>.
    /// </summary>
    /// <remarks>
    /// Values are the dBase / FoxPro language-driver ID (LDID) table. Several drivers share one
    /// code page, so the member name is the language, not the page. Only the drivers this reader
    /// maps are listed; every other byte is unknown and falls back to the default page.
    /// </remarks>
    internal enum LanguageDriver : byte
    {
        // IBM437
        DosUnitedStates = 0x01,
        DosDutch = 0x09,
        DosFinnish = 0x0B,
        DosFrench = 0x0D,
        DosGerman = 0x0F,
        DosItalian = 0x11,
        DosSwedish = 0x15,
        DosSpanish = 0x18,
        DosEnglishBritain = 0x19,
        DosEnglishUnitedStates = 0x1B,

        // IBM850
        DosInternational = 0x02,
        DosDutchSecondary = 0x0A,
        DosFrenchSecondary = 0x0E,
        DosGermanSecondary = 0x10,
        DosItalianSecondary = 0x12,
        DosSpanishSecondary = 0x14,
        DosSwedishSecondary = 0x16,
        DosEnglishBritainSecondary = 0x1A,
        DosFrenchCanadianSecondary = 0x1D,
        DosPortugueseSecondary = 0x25,
        DosEnglishUnitedStatesSecondary = 0x37,

        // Windows-1252
        WindowsAnsi = 0x03,
        Ansi = 0x57,
        WesternEuropeanAnsi = 0x58,
        SpanishAnsi = 0x59,

        // IBM865
        DosDanish = 0x08,
        DosNorwegian = 0x17,
        DosNordic = 0x66,

        // Shift-JIS
        JapaneseShiftJis = 0x13,
        JapaneseWindows = 0x7B,

        // IBM863
        DosFrenchCanadian = 0x1C,
        DosFrenchCanadianAlternate = 0x6C,

        // IBM852
        DosCzech = 0x1F,
        DosHungarian = 0x22,
        DosPolish = 0x23,
        DosRomanian = 0x40,
        DosEasternEuropean = 0x64,

        // IBM860
        DosPortuguese = 0x24,

        // IBM866
        DosRussian = 0x26,
        DosRussianSecondary = 0x65,

        // GBK
        ChineseGbk = 0x4D,
        ChineseSimplifiedWindows = 0x7A,

        // EUC-KR
        KoreanAnsiOem = 0x4E,
        KoreanWindows = 0x79,

        // Big5
        ChineseBig5 = 0x4F,
        ChineseTraditionalWindows = 0x78,

        // Windows-874
        ThaiAnsiOem = 0x50,
        ThaiWindows = 0x7C,

        // IBM861
        DosIcelandic = 0x67,

        // IBM737
        DosGreek = 0x6A,

        // IBM857
        DosTurkish = 0x6B,

        // Windows-1255
        HebrewWindows = 0x7D,

        // Windows-1256
        ArabicWindows = 0x7E,

        // Macintosh
        MacStandard = 0x04,
        MacRussian = 0x96,
        MacEasternEuropean = 0x97,
        MacGreek = 0x98,

        // Windows-1250 through 1257
        WindowsEasternEuropean = 0xC8,
        WindowsRussian = 0xC9,
        WindowsTurkish = 0xCA,
        WindowsGreek = 0xCB,
        WindowsBaltic = 0xCC,
    }
}

namespace VfpReader
{
    /// <summary>
    /// A code page a DBF table can record in its language-driver byte (header offset 29). The
    /// numeric values are the Windows code page identifiers, the same numbers
    /// <see cref="System.Text.Encoding.GetEncoding(int)"/> accepts.
    /// </summary>
    /// <remarks>
    /// This is the set of pages <see cref="Internal.CodePageMap"/> can return from a language
    /// driver, plus <see cref="Utf8"/> for callers who override <see cref="DbfReadOptions.Encoding"/>.
    /// It is not exhaustive: the .NET base class library has no code-page enum and no notion of the
    /// DBF language-driver byte, so an encoding the caller forces is reported as its code page value
    /// even when that value has no member here.
    /// </remarks>
    public enum CodePage
    {
        // IBM PC (DOS) OEM code pages.
        Ibm437 = 437,
        Ibm850 = 850,
        Ibm852 = 852,
        Ibm857 = 857,
        Ibm860 = 860,
        Ibm861 = 861,
        Ibm863 = 863,
        Ibm865 = 865,
        Ibm866 = 866,
        Ibm737 = 737,

        // Windows ANSI code pages.
        Windows1250 = 1250,
        Windows1251 = 1251,
        Windows1252 = 1252,
        Windows1253 = 1253,
        Windows1254 = 1254,
        Windows1255 = 1255,
        Windows1256 = 1256,
        Windows1257 = 1257,
        Windows874 = 874,

        // East Asian multibyte code pages.
        ShiftJis = 932,
        Gbk = 936,
        EucKr = 949,
        Big5 = 950,

        // Macintosh code pages.
        MacRoman = 10000,
        MacGreek = 10006,
        MacCyrillic = 10007,
        MacCentralEurope = 10029,

        /// <summary>
        /// UTF-8. Never recorded by a language driver; present so an explicit
        /// <see cref="DbfReadOptions.Encoding"/> override is a named value.
        /// </summary>
        Utf8 = 65001,
    }
}

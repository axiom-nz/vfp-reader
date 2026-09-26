using VfpReader.Tests.Fixtures;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// The seam between the golden rows and the reader's streaming API. It is intentionally not
    /// implemented: Phase 2 adds <c>DbfTable.ReadRows()</c>, Phase 3 adds memo values, Phase 4
    /// adds null flags and varlength. Each phase implements the two methods below and removes the
    /// matching skips in <c>RowGoldenTests</c>.
    /// </summary>
    internal static class GoldenRows
    {
        /// <summary>
        /// Compares every CSV row against the reader. Cells are canonicalised per field type so
        /// the reference dump's text form and the reader's typed value meet in the middle.
        /// </summary>
        public static void CompareCsv(GoldenFixture fixture, DbfTable table)
        {
            throw new System.NotSupportedException(
                "Phase 2: DbfTable has no streaming row API yet, so '" + fixture.Name + "' cannot be compared.");
        }

        /// <summary>
        /// Compares one YAML record against the reader, by position over the visible fields. A
        /// table whose memo file is absent (<c>dbase_83_missing_memo</c>) reads memo as empty,
        /// never as an error.
        /// </summary>
        public static void CompareRecord(GoldenFixture fixture, string recordPath, DbfTable table)
        {
            throw new System.NotSupportedException(
                "Phase 3: memo values are not available yet, so '" + recordPath + "' cannot be compared.");
        }
    }
}

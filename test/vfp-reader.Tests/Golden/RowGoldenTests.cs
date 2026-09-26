using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// The phase-gated golden row tests. They are compiled and discovered from Phase 1 but skip
    /// until the reader can actually produce the values, so the default gate stays green while the
    /// pending work stays visible. Each phase removes the matching skips.
    /// </summary>
    public class RowGoldenTests
    {
        [Fact]
        public void dbase_31_nullflags_csv_matches()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_31_nullflags");
            using var table = fixture.Open();
            GoldenRows.CompareCsv(fixture, table);
        }

        [Theory]
        [InlineData("dbase_03")]
        [InlineData("dbase_03_cyrillic")]
        [InlineData("cp1251")]
        [InlineData("dbase_31")]
        public void Fixed_records_stream_to_the_declared_count(string fixtureName)
        {
            GoldenFixture fixture = GoldenManifest.Find(fixtureName);
            ExpectedSummary summary = SummaryFile.Read(fixture.SummaryPath!);

            using var table = fixture.Open();

            long count = 0;
            foreach (VfpRow row in table.ReadRows())
            {
                count++;
            }

            Assert.Equal(summary.RecordCount, count);
        }

        [Fact(Skip = "Phase 3: memo reading is not implemented")]
        public void dbase_83_record_0_matches()
        {
            CompareRecord("dbase_83", "dbase_83_record_0.yml");
        }

        [Fact(Skip = "Phase 3: memo reading is not implemented")]
        public void dbase_83_record_9_matches()
        {
            CompareRecord("dbase_83", "dbase_83_record_9.yml");
        }

        [Fact(Skip = "Phase 3: memo reading is not implemented; absent memo must read as empty")]
        public void dbase_83_missing_memo_record_0_matches()
        {
            CompareRecord("dbase_83_missing_memo", "dbase_83_missing_memo_record_0.yml");
        }

        private static void CompareRecord(string fixtureName, string recordFile)
        {
            GoldenFixture fixture = GoldenManifest.Find(fixtureName);
            using var dbf = fixture.Open();
            GoldenRows.CompareRecord(fixture, FixtureLocator.PathOf(fixture.Directory, recordFile), dbf);
        }
    }
}

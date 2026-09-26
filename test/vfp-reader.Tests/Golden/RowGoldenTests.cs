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

        [Fact]
        public void dbase_32_varlength_value_decodes()
        {
            // The real Visual FoxPro 0x32 table: NAME is a Varchar(250) whose hidden _NullFlags
            // field has its varlength bit set (0x01), so the value length is the field's last byte
            // (0x0e = 14) and the value is "Bad Meets Evil".
            GoldenFixture fixture = GoldenManifest.Find("dbase_32");
            using var table = fixture.Open();

            VfpRow row = Assert.Single(table.ReadRows());
            Assert.Equal("Bad Meets Evil", row["NAME"]);
        }

        [Fact]
        public void dbase_83_record_0_matches()
        {
            CompareRecord("dbase_83", "dbase_83_record_0.yml");
        }

        [Fact]
        public void dbase_83_record_9_matches()
        {
            CompareRecord("dbase_83", "dbase_83_record_9.yml");
        }

        [Fact]
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

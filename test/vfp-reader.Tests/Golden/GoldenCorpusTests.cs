using System;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// Keeps the golden files honest: a CSV or YAML oracle must line up with the schema the
    /// reader reports, or a future value comparison would be comparing unrelated columns. These
    /// run from Phase 1 and do not need the row API.
    /// </summary>
    public class GoldenCorpusTests
    {
        [Fact]
        public void Csv_columns_line_up_with_the_schema()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_31_nullflags");
            GoldenCsv csv = GoldenCsv.Read(fixture.CsvPath!);

            using var table = fixture.Open();

            Assert.True(csv.Header.Count >= table.Schema.FieldCount);
            for (int i = 0; i < table.Schema.FieldCount; i++)
            {
                Assert.Equal(table.Schema.Fields[i].Name, csv.Header[i], ignoreCase: true);
            }

            // The one extra column the ruby-dbf dumps keep is the hidden null-flags field.
            for (int i = table.Schema.FieldCount; i < csv.Header.Count; i++)
            {
                Assert.Equal("_NullFlags", csv.Header[i], ignoreCase: true);
            }

            Assert.Equal(table.Schema.RecordCount, csv.Rows.Count);
        }

        [Theory]
        [InlineData("dbase_83", "dbase_83_record_0.yml")]
        [InlineData("dbase_83", "dbase_83_record_9.yml")]
        [InlineData("dbase_83", "dbase_83_missing_memo_record_0.yml")]
        public void Yaml_records_have_one_value_per_visible_field(string fixtureName, string recordFile)
        {
            GoldenFixture fixture = GoldenManifest.Find(fixtureName);
            GoldenRecord record = GoldenRecord.Read(FixtureLocator.PathOf(fixture.Directory, recordFile));

            using var table = fixture.Open();

            Assert.Equal(table.Schema.FieldCount, record.Values.Count);
        }
    }
}

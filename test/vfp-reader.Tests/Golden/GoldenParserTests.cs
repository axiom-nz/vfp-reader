using System;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// Unit-tests the golden-file readers with inline samples, the same way <c>DbfBuilder</c>
    /// pins the binary format. If a real golden stops parsing, this fails first and points at the
    /// parser rather than at the reader.
    /// </summary>
    public class GoldenParserTests
    {
        [Fact]
        public void Summary_parses_header_and_fields_and_hides_nullflags()
        {
            string[] lines =
            {
                string.Empty,
                "Database: demo.dbf",
                "Type: (31) Visual FoxPro with AutoIncrement field",
                "Memo File: false",
                "Records: 77",
                string.Empty,
                "Fields:",
                "Name             Type       Length     Decimal",
                "------------------------------------------------------------------------------",
                "ID               I          4          0         ",
                "NAME             C          20         0         ",
                "_NullFlags       0          1          0         ",
            };

            ExpectedSummary summary = SummaryFile.Parse(lines, "inline");

            Assert.Equal("demo.dbf", summary.Database);
            Assert.Equal(0x31, summary.Version);
            Assert.False(summary.HasMemo);
            Assert.Equal(77L, summary.RecordCount);
            Assert.Equal(3, summary.Fields.Count);
            Assert.Equal(2, summary.VisibleFields.Count);
            Assert.Equal("NAME", summary.VisibleFields[1].Name);
            Assert.Equal('C', summary.VisibleFields[1].Type);
        }

        [Fact]
        public void Csv_parses_bom_quotes_and_empty_cells()
        {
            string text = "\uFEFFA,B,C\r\n\"x,y\",\"a\"\"b\",\r\n1,2,3\r\n";

            GoldenCsv csv = GoldenCsv.Parse(text, "inline");

            Assert.Equal(new[] { "A", "B", "C" }, csv.Header);
            Assert.Equal(2, csv.Rows.Count);
            Assert.Equal("x,y", csv.Rows[0][0]);
            Assert.Equal("a\"b", csv.Rows[0][1]);
            Assert.Equal(string.Empty, csv.Rows[0][2]);
            Assert.Equal("3", csv.Rows[1][2]);
        }

        [Fact]
        public void Yaml_parses_sequence_scalars_bools_and_empty_values()
        {
            string yaml =
                "---\n"
                + "- 87\n"
                + "- '1'\n"
                + "- hello\n"
                + "- true\n"
                + "- \n"
                + "- \"line one\\nline two\"\n";

            GoldenRecord record = GoldenRecord.Parse(yaml, "inline");

            Assert.Equal(6, record.Values.Count);
            Assert.Equal("87", record.Values[0]);
            Assert.Equal("1", record.Values[1]);
            Assert.True(GoldenRecord.AsBoolean(record.Values[3]));
            Assert.Equal(string.Empty, record.Values[4]);
            Assert.Contains("line one", record.Values[5]);
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Which reader capability a golden case needs before it can pass. The phase-gated tests carry
    /// this as a <c>Skip</c> reason; when a phase lands its tests have the reason removed.
    /// </summary>
    internal enum GoldenCapability
    {
        /// <summary>Header, layout and schema only. Available after Phase 1.</summary>
        Schema,

        /// <summary>Streaming fixed-width record decoding. Phase 2.</summary>
        FixedRecords,

        /// <summary>Memo (.fpt/.dbt) reading. Phase 3.</summary>
        Memo,

        /// <summary>_NullFlags and V/Q varlength fields. Phase 4.</summary>
        NullFlagsAndVarlength,
    }

    /// <summary>One real fixture table and the golden outputs that ship with it.</summary>
    internal sealed class GoldenFixture
    {
        public GoldenFixture(
            string name,
            string directory,
            string table,
            string? summary,
            string? csv,
            string[] records,
            GoldenCapability rowsCapability,
            CodePage? encodingCodePage = null,
            string? skipReason = null)
        {
            Name = name;
            Directory = directory;
            Table = table;
            Summary = summary;
            Csv = csv;
            Records = records;
            RowsCapability = rowsCapability;
            EncodingCodePage = encodingCodePage;
            SkipReason = skipReason;
        }

        public string Name { get; }

        public string Directory { get; }

        public string Table { get; }

        public string? Summary { get; }

        public string? Csv { get; }

        public string[] Records { get; }

        public GoldenCapability RowsCapability { get; }

        /// <summary>
        /// An explicit code page for fixtures whose bytes need it. <c>dbase_03_cyrillic</c> has no
        /// language driver but stores UTF-8, and the golden was produced with UTF-8, so the test
        /// must pass it; the reader's documented no-driver default is 1252.
        /// </summary>
        public CodePage? EncodingCodePage { get; }

        /// <summary>Set when this fixture is a known reader gap and its golden must skip.</summary>
        public string? SkipReason { get; }

        public string TablePath
        {
            get { return FixtureLocator.PathOf(Directory, Table); }
        }

        public string? SummaryPath
        {
            get { return Summary is null ? null : FixtureLocator.PathOf(Directory, Summary); }
        }

        public string? CsvPath
        {
            get { return Csv is null ? null : FixtureLocator.PathOf(Directory, Csv); }
        }

        public IReadOnlyList<string> RecordPaths
        {
            get
            {
                var paths = new List<string>();
                foreach (string record in Records)
                {
                    paths.Add(FixtureLocator.PathOf(Directory, record));
                }

                return paths;
            }
        }

        /// <summary>Opens the table with any fixture-specific encoding override applied.</summary>
        public VfpTable Open()
        {
            var options = new VfpReadOptions();
            if (EncodingCodePage is CodePage codePage)
            {
                options.Encoding = Encoding.GetEncoding((int)codePage);
            }

            return VfpTable.Open(TablePath, options);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// The single place fixtures are registered. Adding a table or a golden file is a change here,
    /// never a directory scan, so the test set is deterministic and reviewable.
    /// </summary>
    internal static class GoldenManifest
    {
        public static readonly IReadOnlyList<GoldenFixture> Fixtures = new[]
        {
            new GoldenFixture("dbase_02", "ruby-dbf", "dbase_02.dbf", "dbase_02_summary.txt", null, new string[0], GoldenCapability.FixedRecords, skipReason: "reader gap: FoxBase 0x02 uses an 8-byte header and 16-byte field descriptors"),
            new GoldenFixture("dbase_03", "ruby-dbf", "dbase_03.dbf", "dbase_03_summary.txt", null, new string[0], GoldenCapability.FixedRecords),
            new GoldenFixture("dbase_03_cyrillic", "ruby-dbf", "dbase_03_cyrillic.dbf", "dbase_03_cyrillic_summary.txt", null, new string[0], GoldenCapability.FixedRecords, encodingCodePage: CodePage.Utf8),
            new GoldenFixture("cp1251", "ruby-dbf", "cp1251.dbf", "cp1251_summary.txt", null, new string[0], GoldenCapability.FixedRecords),
            new GoldenFixture("dbase_30", "ruby-dbf", "dbase_30.dbf", "dbase_30_summary.txt", null, new string[0], GoldenCapability.Memo),
            new GoldenFixture("dbase_31", "ruby-dbf", "dbase_31.dbf", "dbase_31_summary.txt", null, new string[0], GoldenCapability.FixedRecords),
            new GoldenFixture("dbase_32", "ruby-dbf", "dbase_32.dbf", "dbase_32_summary.txt", null, new string[0], GoldenCapability.NullFlagsAndVarlength),
            new GoldenFixture("dbase_83", "ruby-dbf", "dbase_83.dbf", "dbase_83_summary.txt", null, new[] { "dbase_83_record_0.yml", "dbase_83_record_9.yml" }, GoldenCapability.Memo),
            new GoldenFixture("dbase_83_missing_memo", "ruby-dbf", "dbase_83_missing_memo.dbf", null, null, new[] { "dbase_83_missing_memo_record_0.yml" }, GoldenCapability.Memo),
            new GoldenFixture("dbase_8b", "ruby-dbf", "dbase_8b.dbf", "dbase_8b_summary.txt", null, new string[0], GoldenCapability.Memo),
            new GoldenFixture("dbase_f5", "ruby-dbf", "dbase_f5.dbf", "dbase_f5_summary.txt", null, new string[0], GoldenCapability.Memo),
            new GoldenFixture("dbase_31_nullflags", "dbfdatareader", "dbase_31_nullflags.dbf", "dbase_31_nullflags_summary.txt", "dbase_31_nullflags.csv", new string[0], GoldenCapability.NullFlagsAndVarlength),
        };

        /// <summary>The fixtures that carry a schema oracle.</summary>
        public static IEnumerable<GoldenFixture> WithSummary()
        {
            foreach (GoldenFixture fixture in Fixtures)
            {
                if (fixture.Summary is not null)
                {
                    yield return fixture;
                }
            }
        }

        /// <summary>The schema-oracle fixtures whose golden is expected to pass today.</summary>
        public static IEnumerable<GoldenFixture> SupportedWithSummary()
        {
            foreach (GoldenFixture fixture in WithSummary())
            {
                if (fixture.SkipReason is null)
                {
                    yield return fixture;
                }
            }
        }

        public static GoldenFixture Find(string name)
        {
            foreach (GoldenFixture fixture in Fixtures)
            {
                if (fixture.Name == name)
                {
                    return fixture;
                }
            }

            throw new KeyNotFoundException("no fixture named " + name);
        }
    }
}

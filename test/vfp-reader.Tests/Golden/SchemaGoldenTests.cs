using System.Collections.Generic;
using System.Linq;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// Pins the reader's schema against every ruby-dbf <c>*_summary.txt</c>. These are the
    /// Phase 1 golden tests: they need only the header and field descriptors, and they fail on
    /// any real fixture the layout logic cannot walk.
    /// </summary>
    public class SchemaGoldenTests
    {
        public static IEnumerable<object[]> Summaries()
        {
            return GoldenManifest.SupportedWithSummary().Select(fixture => new object[] { fixture.Name });
        }

        [Theory]
        [MemberData(nameof(Summaries))]
        public void Summary_matches(string fixtureName)
        {
            GoldenFixture fixture = GoldenManifest.Find(fixtureName);
            ExpectedSummary expected = SummaryFile.Read(fixture.SummaryPath!);

            using var table = fixture.Open();
            GoldenAssert.Schema(fixtureName, expected, table.Schema);
        }

        [Fact]
        public void Corpus_is_copied_beside_the_tests()
        {
            Assert.True(FixtureLocator.Copied, "fixtures were not copied to " + FixtureLocator.PathOf());
        }

        // Known gap surfaced by the corpus. FoxBase 0x02 has an 8-byte header, no code-page
        // byte, an implied 521-byte header length and 16-byte field descriptors. Remove this
        // skip when that layout lands.
        [Fact(Skip = "Phase 1 gap: FoxBase 0x02 8-byte header / 16-byte descriptors are not parsed")]
        public void FoxBase_dbase_02_summary_matches()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_02");
            ExpectedSummary expected = SummaryFile.Read(fixture.SummaryPath!);

            using var table = fixture.Open();
            GoldenAssert.Schema("dbase_02", expected, table.Schema);
        }
    }
}

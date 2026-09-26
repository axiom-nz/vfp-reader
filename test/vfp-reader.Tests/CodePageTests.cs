using System;
using System.Text;
using VfpReader.Internal;
using Xunit;

namespace VfpReader.Tests
{
    public class CodePageTests
    {
        [Theory]
        [InlineData(0x01, CodePage.Ibm437)]
        [InlineData(0x09, CodePage.Ibm437)]
        [InlineData(0x0B, CodePage.Ibm437)]
        [InlineData(0x0D, CodePage.Ibm437)]
        [InlineData(0x0F, CodePage.Ibm437)]
        [InlineData(0x11, CodePage.Ibm437)]
        [InlineData(0x15, CodePage.Ibm437)]
        [InlineData(0x18, CodePage.Ibm437)]
        [InlineData(0x19, CodePage.Ibm437)]
        [InlineData(0x1B, CodePage.Ibm437)]
        [InlineData(0x02, CodePage.Ibm850)]
        [InlineData(0x0A, CodePage.Ibm850)]
        [InlineData(0x0E, CodePage.Ibm850)]
        [InlineData(0x10, CodePage.Ibm850)]
        [InlineData(0x12, CodePage.Ibm850)]
        [InlineData(0x14, CodePage.Ibm850)]
        [InlineData(0x16, CodePage.Ibm850)]
        [InlineData(0x1A, CodePage.Ibm850)]
        [InlineData(0x1D, CodePage.Ibm850)]
        [InlineData(0x25, CodePage.Ibm850)]
        [InlineData(0x37, CodePage.Ibm850)]
        [InlineData(0x03, CodePage.Windows1252)]
        [InlineData(0x57, CodePage.Windows1252)]
        [InlineData(0x58, CodePage.Windows1252)]
        [InlineData(0x59, CodePage.Windows1252)]
        [InlineData(0x04, CodePage.MacRoman)]
        [InlineData(0x98, CodePage.MacGreek)]
        [InlineData(0x08, CodePage.Ibm865)]
        [InlineData(0x17, CodePage.Ibm865)]
        [InlineData(0x66, CodePage.Ibm865)]
        [InlineData(0x13, CodePage.ShiftJis)]
        [InlineData(0x7B, CodePage.ShiftJis)]
        [InlineData(0x1C, CodePage.Ibm863)]
        [InlineData(0x6C, CodePage.Ibm863)]
        [InlineData(0x1F, CodePage.Ibm852)]
        [InlineData(0x22, CodePage.Ibm852)]
        [InlineData(0x23, CodePage.Ibm852)]
        [InlineData(0x40, CodePage.Ibm852)]
        [InlineData(0x64, CodePage.Ibm852)]
        [InlineData(0x24, CodePage.Ibm860)]
        [InlineData(0x26, CodePage.Ibm866)]
        [InlineData(0x65, CodePage.Ibm866)]
        [InlineData(0x4D, CodePage.Gbk)]
        [InlineData(0x7A, CodePage.Gbk)]
        [InlineData(0x4E, CodePage.EucKr)]
        [InlineData(0x79, CodePage.EucKr)]
        [InlineData(0x4F, CodePage.Big5)]
        [InlineData(0x78, CodePage.Big5)]
        [InlineData(0x50, CodePage.Windows874)]
        [InlineData(0x7C, CodePage.Windows874)]
        [InlineData(0x67, CodePage.Ibm861)]
        [InlineData(0x6A, CodePage.Ibm737)]
        [InlineData(0x6B, CodePage.Ibm857)]
        [InlineData(0x7D, CodePage.Windows1255)]
        [InlineData(0x7E, CodePage.Windows1256)]
        [InlineData(0x96, CodePage.MacCyrillic)]
        [InlineData(0x97, CodePage.MacCentralEurope)]
        [InlineData(0xC8, CodePage.Windows1250)]
        [InlineData(0xC9, CodePage.Windows1251)]
        [InlineData(0xCA, CodePage.Windows1254)]
        [InlineData(0xCB, CodePage.Windows1253)]
        [InlineData(0xCC, CodePage.Windows1257)]
        public void Language_driver_maps_to_code_page(int driver, CodePage expected)
        {
            Assert.Equal<CodePage?>(expected, CodePageMap.FromLanguageDriver((byte)driver));
        }

        [Theory]
        [InlineData(0x00)]
        [InlineData(0x05)]
        [InlineData(0x06)]
        [InlineData(0x07)]
        [InlineData(0x99)]
        [InlineData(0xFF)]
        public void Unknown_language_driver_has_no_mapping(int driver)
        {
            Assert.Null(CodePageMap.FromLanguageDriver((byte)driver));
        }

        /// <summary>
        /// Keeps the hand-written <see cref="CodePage"/> values honest: every member must be the
        /// code page the runtime reports for that number.
        /// </summary>
        [Fact]
        public void Every_code_page_matches_the_runtime()
        {
            foreach (CodePage page in Enum.GetValues<CodePage>())
            {
                Assert.Equal((int)page, Encoding.GetEncoding((int)page).CodePage);
            }
        }
    }
}

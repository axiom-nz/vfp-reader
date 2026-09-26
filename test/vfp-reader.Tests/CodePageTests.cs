using VfpReader.Internal;
using Xunit;

namespace VfpReader.Tests
{
    public class CodePageTests
    {
        [Theory]
        [InlineData(0x01, 437)]
        [InlineData(0x09, 437)]
        [InlineData(0x0B, 437)]
        [InlineData(0x0D, 437)]
        [InlineData(0x0F, 437)]
        [InlineData(0x11, 437)]
        [InlineData(0x15, 437)]
        [InlineData(0x18, 437)]
        [InlineData(0x19, 437)]
        [InlineData(0x1B, 437)]
        [InlineData(0x02, 850)]
        [InlineData(0x0A, 850)]
        [InlineData(0x0E, 850)]
        [InlineData(0x10, 850)]
        [InlineData(0x12, 850)]
        [InlineData(0x14, 850)]
        [InlineData(0x16, 850)]
        [InlineData(0x1A, 850)]
        [InlineData(0x1D, 850)]
        [InlineData(0x25, 850)]
        [InlineData(0x37, 850)]
        [InlineData(0x03, 1252)]
        [InlineData(0x57, 1252)]
        [InlineData(0x58, 1252)]
        [InlineData(0x59, 1252)]
        [InlineData(0x04, 10000)]
        [InlineData(0x98, 10006)]
        [InlineData(0x08, 865)]
        [InlineData(0x17, 865)]
        [InlineData(0x66, 865)]
        [InlineData(0x13, 932)]
        [InlineData(0x7B, 932)]
        [InlineData(0x1C, 863)]
        [InlineData(0x6C, 863)]
        [InlineData(0x1F, 852)]
        [InlineData(0x22, 852)]
        [InlineData(0x23, 852)]
        [InlineData(0x40, 852)]
        [InlineData(0x64, 852)]
        [InlineData(0x24, 860)]
        [InlineData(0x26, 866)]
        [InlineData(0x65, 866)]
        [InlineData(0x4D, 936)]
        [InlineData(0x7A, 936)]
        [InlineData(0x4E, 949)]
        [InlineData(0x79, 949)]
        [InlineData(0x4F, 950)]
        [InlineData(0x78, 950)]
        [InlineData(0x50, 874)]
        [InlineData(0x7C, 874)]
        [InlineData(0x67, 861)]
        [InlineData(0x6A, 737)]
        [InlineData(0x6B, 857)]
        [InlineData(0x7D, 1255)]
        [InlineData(0x7E, 1256)]
        [InlineData(0x96, 10007)]
        [InlineData(0x97, 10029)]
        [InlineData(0xC8, 1250)]
        [InlineData(0xC9, 1251)]
        [InlineData(0xCA, 1254)]
        [InlineData(0xCB, 1253)]
        [InlineData(0xCC, 1257)]
        public void Language_driver_maps_to_code_page(int driver, int expected)
        {
            Assert.Equal(expected, CodePageMap.FromLanguageDriver((byte)driver));
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
    }
}

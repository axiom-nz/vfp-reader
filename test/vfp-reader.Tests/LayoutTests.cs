using System.IO;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests
{
    public class LayoutTests
    {
        [Fact]
        public void Fields_are_laid_out_end_to_end_after_the_deletion_flag()
        {
            var builder = new DbfBuilder();
            builder.AddField("A", 'C', 5);
            builder.AddField("B", 'I', 4);
            builder.AddField("C", 'D', 8);

            using VfpTable table = VfpTable.Open(new MemoryStream(builder.Build()));

            Assert.Equal(1, table.Schema.Fields[0].Offset);
            Assert.Equal(5, table.Schema.Fields[0].Width);
            Assert.Equal(6, table.Schema.Fields[1].Offset);
            Assert.Equal(4, table.Schema.Fields[1].Width);
            Assert.Equal(10, table.Schema.Fields[2].Offset);
            Assert.Equal(8, table.Schema.Fields[2].Width);
            Assert.Equal(1 + 5 + 4 + 8, table.Schema.RecordLength);
        }

        [Fact]
        public void Wide_character_fields_are_retried_when_the_normal_layout_does_not_fit()
        {
            // C(10,1) occupies 10 bytes normally and 10 + 1 * 256 = 266 bytes under the
            // FoxPro 2.x wide-character convention. The declared record length only fits the wide
            // layout, so the reader must retry with it.
            var builder = new DbfBuilder { ExplicitRecordLength = 1 + 266 + 4 };
            builder.AddField("WIDE", 'C', 10, decimals: 1);
            builder.AddField("ID", 'I', 4);
            builder.AddRecord(false, new byte[1 + 266 + 4 - 1]);

            using VfpTable table = VfpTable.Open(new MemoryStream(builder.Build()));

            Assert.Equal(266, table.Schema.Fields[0].Width);
            Assert.Equal(1, table.Schema.Fields[0].Offset);
            Assert.Equal(267, table.Schema.Fields[1].Offset);
            Assert.Equal(4, table.Schema.Fields[1].Width);
        }

        [Fact]
        public void Normal_layout_is_used_when_wide_does_not_fit()
        {
            // C(10,1) can be read as a 266-byte wide-character field, but this table declares the
            // ordinary 10-byte width (1 deletion flag + 10 + 4 = 15). The wide convention does not
            // fit, so the reader must fall back to the normal one instead of rejecting the table.
            var builder = new DbfBuilder { ExplicitRecordLength = 1 + 10 + 4 };
            builder.AddField("TEXT", 'C', 10, decimals: 1);
            builder.AddField("ID", 'I', 4);
            builder.AddRecord(false, new byte[1 + 10 + 4 - 1]);

            using VfpTable table = VfpTable.Open(new MemoryStream(builder.Build()));

            Assert.Equal(10, table.Schema.Fields[0].Width);
            Assert.Equal(1, table.Schema.Fields[0].Offset);
            Assert.Equal(11, table.Schema.Fields[1].Offset);
            Assert.Equal(4, table.Schema.Fields[1].Width);
        }

        [Fact]
        public void Record_length_too_small_for_the_fields_throws()
        {
            var builder = new DbfBuilder { ExplicitRecordLength = 1 + 5 };
            builder.AddField("A", 'C', 10);
            builder.AddField("B", 'I', 4);
            builder.AddRecord(false, new byte[5]);

            Assert.Throws<VfpFormatException>(() => VfpTable.Open(new MemoryStream(builder.Build())));
        }
    }
}

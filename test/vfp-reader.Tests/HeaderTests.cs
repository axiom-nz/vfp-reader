using System;
using System.IO;
using System.Text;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests
{
    public class HeaderTests
    {
        private static DbfTable Open(DbfBuilder builder)
        {
            return DbfTable.Open(new MemoryStream(builder.Build()));
        }

        [Fact]
        public void Reads_version_record_count_and_lengths()
        {
            var builder = new DbfBuilder { Version = 0x30, Year = 124, Month = 6, Day = 15, LanguageDriver = 0x03 };
            builder.AddField("NAME", 'C', 20);
            builder.AddField("AGE", 'N', 3);
            builder.AddRecord(false, new byte[23]);

            using DbfTable table = Open(builder);

            Assert.Equal(0x30, table.Schema.Version);
            Assert.Equal(1L, table.Schema.RecordCount);
            Assert.Equal(32 + (2 * 32) + 1 + 263, table.Schema.HeaderLength);
            Assert.Equal(24, table.Schema.RecordLength);
            Assert.Equal(new DateTime(2024, 6, 15), table.Schema.LastUpdate);
            Assert.Equal(2, table.Schema.FieldCount);
        }

        [Fact]
        public void Record_count_uses_four_bytes()
        {
            // The record count at bytes 4..7 is a 32-bit little-endian value. Read as 16 bits
            // the same header would report 65535.
            var builder = new DbfBuilder();
            builder.AddField("A", 'C', 4);
            byte[] bytes = builder.Build();
            bytes[4] = 0xFF;
            bytes[5] = 0xFF;
            bytes[6] = 0xFF;
            bytes[7] = 0xFF;

            using DbfTable table = DbfTable.Open(new MemoryStream(bytes));

            Assert.Equal(4294967295L, table.Schema.RecordCount);
        }

        [Fact]
        public void Reads_field_properties()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 20, nullable: true);
            builder.AddField("AMOUNT", 'N', 12, decimals: 2);
            builder.AddField("STAMP", 'T', 8);

            using DbfTable table = Open(builder);

            DbfField name = table.Schema.Fields[0];
            Assert.Equal("NAME", name.Name);
            Assert.Equal(DbfFieldType.Character, name.Type);
            Assert.Equal(20, name.Length);
            Assert.True(name.IsNullable);

            DbfField amount = table.Schema.Fields[1];
            Assert.Equal(DbfFieldType.Numeric, amount.Type);
            Assert.Equal(2, amount.Decimals);
            Assert.False(amount.IsNullable);

            Assert.Equal(DbfFieldType.DateTime, table.Schema.Fields[2].Type);
        }

        [Fact]
        public void Null_flags_field_is_hidden_and_nullable_bits_are_assigned()
        {
            var builder = new DbfBuilder();
            builder.AddField("A", 'C', 5, nullable: true);
            builder.AddField("B", 'N', 10, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            using DbfTable table = Open(builder);

            Assert.Equal(2, table.Schema.FieldCount);
            Assert.Equal(-1, table.Schema.FieldIndex("_NullFlags"));
            Assert.Equal(0, table.Schema.Fields[0].NullBit!.Value);
            Assert.Equal(1, table.Schema.Fields[1].NullBit!.Value);
        }

        [Fact]
        public void Nullflags_detection_is_case_insensitive()
        {
            // The hidden field is recognised whatever its capitalisation; only the exact spelling
            // is hidden today, so an Ordinal comparison would expose this field.
            var builder = new DbfBuilder();
            builder.AddField("A", 'C', 5, nullable: true);
            builder.AddField("_nullflags", '0', 1, binary: true, system: true);

            using DbfTable table = Open(builder);

            Assert.Equal(1, table.Schema.FieldCount);
            Assert.Equal(-1, table.Schema.FieldIndex("_NullFlags"));
            Assert.Equal(0, table.Schema.Fields[0].NullBit!.Value);
        }

        [Fact]
        public void Nullflags_only_table_throws()
        {
            // The post-hide count, not the raw descriptor count, is what must be non-empty.
            var builder = new DbfBuilder();
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            DbfFormatException ex = Assert.Throws<DbfFormatException>(() => Open(builder));

            Assert.Contains("no fields", ex.Message);
        }

        [Fact]
        public void Backlink_is_exposed_as_database_path()
        {
            var builder = new DbfBuilder { Version = 0x30, DatabasePath = @"..\data\vfp.dbc" };
            builder.AddField("ID", 'I', 4);

            using DbfTable table = Open(builder);

            Assert.Equal(@"..\data\vfp.dbc", table.Schema.DatabasePath);
        }

        [Fact]
        public void Free_vfp_table_has_empty_database_path()
        {
            var builder = new DbfBuilder { Version = 0x30 };
            builder.AddField("ID", 'I', 4);

            using DbfTable table = Open(builder);

            Assert.Equal(string.Empty, table.Schema.DatabasePath);
        }

        [Fact]
        public void Dbase_table_has_no_backlink()
        {
            var builder = new DbfBuilder { Version = 0x03 };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.Equal(string.Empty, table.Schema.DatabasePath);
            Assert.Equal(32 + 32 + 1, table.Schema.HeaderLength);
        }

        [Fact]
        public void Memo_field_sets_has_memo()
        {
            var builder = new DbfBuilder { Version = 0x30 };
            builder.AddField("NOTES", 'M', 10);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.HasMemo);
        }

        [Fact]
        public void Memo_version_sets_has_memo_even_without_a_memo_field()
        {
            var builder = new DbfBuilder { Version = 0x83 };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.HasMemo);
        }

        [Fact]
        public void Memo_version_fb_sets_has_memo()
        {
            // 0xFB is the FoxBASE+/dBASE IV with memo spelling; it has no memo field in this
            // table, so the version alone must set HasMemo.
            var builder = new DbfBuilder { Version = 0xFB };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.HasMemo);
        }

        [Theory]
        [InlineData('G')]
        [InlineData('P')]
        [InlineData('W')]
        public void Binary_memo_types_set_has_memo(char type)
        {
            // General/Picture/Blob fields live in the memo file even on a non-memo version.
            var builder = new DbfBuilder { Version = 0x03 };
            builder.AddField("DATA", type, 10);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.HasMemo);
        }

        [Fact]
        public void Unknown_type_character_maps_to_unknown()
        {
            var builder = new DbfBuilder();
            builder.AddField("WEIRD", 'Z', 4);

            using DbfTable table = Open(builder);

            Assert.Equal(DbfFieldType.Unknown, table.Schema.Fields[0].Type);
        }

        [Fact]
        public void Autoincrement_flags_are_read()
        {
            var builder = new DbfBuilder { Version = 0x31 };
            builder.AddField("ID", 'I', 4, autoIncrementStep: 1, autoIncrementNext: 42);

            using DbfTable table = Open(builder);

            DbfField id = table.Schema.Fields[0];
            Assert.True(id.IsAutoIncrement);
            Assert.Equal(42u, id.AutoIncrementNext);
            Assert.Equal(1, id.AutoIncrementStep);
        }

        [Fact]
        public void Encoding_option_overrides_the_header()
        {
            var builder = new DbfBuilder { LanguageDriver = 0xFF };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = DbfTable.Open(
                new MemoryStream(builder.Build()),
                options: new DbfReadOptions { Encoding = Encoding.GetEncoding(850) });

            Assert.Equal(850, table.Schema.CodePage);
        }

        [Fact]
        public void Unknown_language_driver_defaults_to_1252()
        {
            var builder = new DbfBuilder { LanguageDriver = 0xFF };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.Equal(1252, table.Schema.CodePage);
        }

        [Fact]
        public void Invalid_last_update_reads_as_null()
        {
            var builder = new DbfBuilder { Month = 13, Day = 40 };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.Null(table.Schema.LastUpdate);
        }

        [Fact]
        public void April_31_reads_as_null()
        {
            // Month 4 and day 31 both pass the coarse 1..12 / 1..31 range checks, so only the
            // DaysInMonth guard rejects this date; without it new DateTime would throw.
            var builder = new DbfBuilder { Year = 124, Month = 4, Day = 31 };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.Null(table.Schema.LastUpdate);
        }

        [Fact]
        public void Unterminated_descriptors_throw()
        {
            // 32-byte header + one full 32-byte descriptor = 64 bytes, so the descriptor array
            // reaches the declared header end with no room for the closing 0x0D terminator.
            var bytes = new byte[64];
            bytes[0] = 0x03;
            bytes[8] = 64;
            bytes[10] = 11;
            bytes[32] = (byte)'A';
            bytes[32 + 11] = (byte)'C';
            bytes[32 + 16] = 10;

            DbfFormatException ex = Assert.Throws<DbfFormatException>(
                () => DbfTable.Open(new MemoryStream(bytes)));

            Assert.Contains("not terminated", ex.Message);
        }

        [Fact]
        public void Header_status_flags_are_read()
        {
            // Version 0x30 has no memo and the only field is character, so HasMemo can only come
            // from the header status byte.
            var builder = new DbfBuilder
            {
                Version = 0x30,
                HasIndexFlag = true,
                HasMemoFlag = true,
                IsDatabaseFlag = true,
            };
            builder.AddField("NAME", 'C', 10);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.HasIndex);
            Assert.True(table.Schema.HasMemo);
            Assert.True(table.Schema.IsDatabase);
        }

        [Fact]
        public void Visible_field_system_and_binary_flags_are_read()
        {
            var builder = new DbfBuilder();
            builder.AddField("DATA", 'C', 10, system: true, binary: true);

            using DbfTable table = Open(builder);

            DbfField field = table.Schema.Fields[0];
            Assert.True(field.IsSystem);
            Assert.True(field.IsBinary);
        }

        [Fact]
        public void Binary_field_is_not_autoincrement()
        {
            // 0x04 alone marks raw bytes; only 0x04 | 0x08 marks an autoincrementing field, so a
            // binary field must not report IsAutoIncrement.
            var builder = new DbfBuilder();
            builder.AddField("DATA", 'C', 10, binary: true);

            using DbfTable table = Open(builder);

            Assert.True(table.Schema.Fields[0].IsBinary);
            Assert.False(table.Schema.Fields[0].IsAutoIncrement);
            Assert.Equal(0u, table.Schema.Fields[0].AutoIncrementNext);
        }

        [Fact]
        public void Field_lookup_rejects_null()
        {
            var builder = new DbfBuilder();
            builder.AddField("A", 'C', 4);

            using DbfTable table = Open(builder);

            Assert.Throws<ArgumentNullException>(() => table.Schema.FieldIndex(null!));
            Assert.Throws<ArgumentNullException>(() => table.Schema.FindField(null!));
        }

        [Fact]
        public void Field_lookup_is_case_insensitive()
        {
            var builder = new DbfBuilder();
            builder.AddField("CustomerId", 'I', 4);

            using DbfTable table = Open(builder);

            Assert.Equal(0, table.Schema.FieldIndex("customerid"));
            Assert.NotNull(table.Schema.FindField("CUSTOMERID"));
            Assert.Null(table.Schema.FindField("missing"));
        }

        [Fact]
        public void Truncated_header_throws_with_the_offset()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 10);
            byte[] bytes = builder.Build();
            var truncated = new byte[40];
            Array.Copy(bytes, truncated, truncated.Length);

            DbfFormatException ex = Assert.Throws<DbfFormatException>(
                () => DbfTable.Open(new MemoryStream(truncated)));

            Assert.Equal(8, ex.Offset);
        }

        [Fact]
        public void Header_with_no_fields_throws()
        {
            var bytes = new byte[33];
            bytes[0] = 0x03;
            bytes[8] = 33;
            bytes[10] = 1;
            bytes[32] = 0x0D;

            DbfFormatException ex = Assert.Throws<DbfFormatException>(
                () => DbfTable.Open(new MemoryStream(bytes)));

            Assert.Contains("no fields", ex.Message);
        }

        [Fact]
        public void Descriptor_running_past_the_header_throws()
        {
            var bytes = new byte[33];
            bytes[0] = 0x03;
            bytes[8] = 33;
            bytes[10] = 1;
            bytes[32] = (byte)'C';

            Assert.Throws<DbfFormatException>(() => DbfTable.Open(new MemoryStream(bytes)));
        }

        [Fact]
        public void Record_length_zero_throws()
        {
            var bytes = new byte[34];
            bytes[0] = 0x03;
            bytes[8] = 33;
            bytes[10] = 0;
            bytes[32] = 0x0D;

            DbfFormatException ex = Assert.Throws<DbfFormatException>(
                () => DbfTable.Open(new MemoryStream(bytes)));

            Assert.Contains("record length", ex.Message);
        }

        [Fact]
        public void File_shorter_than_a_header_throws()
        {
            Assert.Throws<DbfFormatException>(() => DbfTable.Open(new MemoryStream(new byte[10])));
        }

        [Fact]
        public void Open_rejects_null_and_unreadable_inputs()
        {
            Assert.Throws<ArgumentNullException>(() => DbfTable.Open((string)null!));
            Assert.Throws<ArgumentNullException>(() => DbfTable.Open((Stream)null!));

            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 10);
            var stream = new MemoryStream(builder.Build());
            stream.Dispose();

            Assert.Throws<ArgumentException>(() => DbfTable.Open(stream));
        }

        [Fact]
        public void Bad_file_opened_by_path_reports_its_path()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "vfp-reader-bad-" + Guid.NewGuid().ToString("N") + ".dbf");
            File.WriteAllBytes(path, new byte[10]);
            try
            {
                DbfFormatException ex = Assert.Throws<DbfFormatException>(() => DbfTable.Open(path));

                Assert.Equal(path, ex.Path);
                Assert.Contains(path, ex.Message);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}

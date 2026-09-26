using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests
{
    public class RecordReaderTests
    {
        private static VfpTable Open(DbfBuilder builder, VfpReadOptions? options = null)
        {
            return TestTable.Open(builder, options);
        }

        [Fact]
        public void Decodes_every_fixed_width_type()
        {
            var builder = new DbfBuilder();
            builder.AddField("CH", 'C', 5);
            builder.AddField("NUM", 'N', 6, decimals: 2);
            builder.AddField("FLT", 'F', 6, decimals: 2);
            builder.AddField("INT", 'I', 4);
            builder.AddField("AUTO", '+', 4);
            builder.AddField("CUR", 'Y', 8);
            builder.AddField("DBL", 'B', 8);
            builder.AddField("DBLO", 'O', 8);
            builder.AddField("LOG", 'L', 1);
            builder.AddField("DAT", 'D', 8);
            builder.AddField("TS", 'T', 8);
            builder.AddField("TSAT", '@', 8);
            builder.AddField("UNK", 'Z', 3);

            var payload = new List<byte>();
            payload.AddRange(Encoding.ASCII.GetBytes("hello"));
            payload.AddRange(Encoding.ASCII.GetBytes(" 12.34"));
            payload.AddRange(Encoding.ASCII.GetBytes("  3.50"));
            payload.AddRange(ByteOrder.LittleEndianInt32(42));
            payload.AddRange(ByteOrder.LittleEndianInt32(7));
            payload.AddRange(ByteOrder.LittleEndianInt64(180000));          // 18.0000
            payload.AddRange(ByteOrder.LittleEndianInt64(BitConverter.DoubleToInt64Bits(1.5)));
            payload.AddRange(ByteOrder.LittleEndianInt64(BitConverter.DoubleToInt64Bits(-2.25)));
            payload.Add((byte)'T');
            payload.AddRange(Encoding.ASCII.GetBytes("20000101"));
            payload.AddRange(DateParts(2451545, 0));
            payload.AddRange(DateParts(2451545, 500));
            payload.AddRange(new byte[] { 1, 2, 3 });

            builder.AddRecord(false, payload.ToArray());

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.IsType<string>(row["CH"]);
            Assert.Equal("hello", row["CH"]);
            Assert.IsType<decimal>(row["NUM"]);
            Assert.Equal(12.34m, row["NUM"]);
            Assert.IsType<double>(row["FLT"]);
            Assert.Equal(3.5, row["FLT"]);
            Assert.IsType<int>(row["INT"]);
            Assert.Equal(42, row["INT"]);
            Assert.IsType<int>(row["AUTO"]);
            Assert.Equal(7, row["AUTO"]);
            Assert.IsType<decimal>(row["CUR"]);
            Assert.Equal(18.0000m, row["CUR"]);
            Assert.IsType<double>(row["DBL"]);
            Assert.Equal(1.5, row["DBL"]);
            Assert.IsType<double>(row["DBLO"]);
            Assert.Equal(-2.25, row["DBLO"]);
            Assert.IsType<bool>(row["LOG"]);
            Assert.Equal(true, (bool?)row["LOG"]);
            Assert.IsType<DateTime>(row["DAT"]);
            Assert.Equal(new DateTime(2000, 1, 1), row["DAT"]);
            Assert.IsType<DateTime>(row["TS"]);
            Assert.Equal(new DateTime(2000, 1, 1), row["TS"]);
            Assert.IsType<DateTime>(row["TSAT"]);
            Assert.Equal(new DateTime(2000, 1, 1).AddMilliseconds(500), row["TSAT"]);
            Assert.IsType<byte[]>(row["UNK"]);
            Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])row["UNK"]!);
            Assert.Equal(13, row.FieldCount);
        }

        [Fact]
        public void Blank_and_unknown_fixed_values_read_as_null()
        {
            var builder = new DbfBuilder();
            builder.AddField("NUM", 'N', 6, decimals: 2);
            builder.AddField("FLT", 'F', 6, decimals: 2);
            builder.AddField("LOG", 'L', 1);
            builder.AddField("DAT", 'D', 8);

            // One all-blank record: every parseable fixed type is empty, and '?' is unknown.
            byte[] payload = Encoding.ASCII.GetBytes("            ?        ");

            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Null(row["NUM"]);
            Assert.Null(row["FLT"]);
            Assert.Null(row["LOG"]);
            Assert.Null(row["DAT"]);
        }

        [Fact]
        public void DateTime_epoch_and_milliseconds_round_trip()
        {
            var builder = new DbfBuilder();
            builder.AddField("TS", 'T', 8);

            var payload = new List<byte>();
            payload.AddRange(DateParts(2451545, 0));
            builder.AddRecord(false, payload.ToArray());

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal(new DateTime(2000, 1, 1), row["TS"]);
        }

        [Fact]
        public void Deleted_records_are_skipped_by_default()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(true, Encoding.ASCII.GetBytes("dead"));
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder);

            VfpRow row = RowReader.Single(table);
            Assert.Equal("live", row["NAME"]);
            Assert.False(row.IsDeleted);
        }

        [Fact]
        public void Include_deleted_returns_the_flag()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(true, Encoding.ASCII.GetBytes("dead"));
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder, new VfpReadOptions { IncludeDeleted = true });

            List<VfpRow> rows = table.ReadRows().ToList();
            Assert.Equal(2, rows.Count);
            Assert.True(rows[0].IsDeleted);
            Assert.Equal("dead", rows[0]["NAME"]);
            Assert.False(rows[1].IsDeleted);
        }

        [Fact]
        public void Record_number_counts_skipped_records()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(true, Encoding.ASCII.GetBytes("dead"));
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder);

            VfpRow row = RowReader.Single(table);
            Assert.Equal(2L, row.RecordNumber);
        }

        [Fact]
        public void Trim_character_fields_trims_by_default()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 6);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("ab \0\0"));

            using VfpTable table = Open(builder);

            Assert.Equal("ab", RowReader.Single(table)["NAME"]);
        }

        [Fact]
        public void Untrimmed_character_fields_keep_the_width()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 6);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("ab \0\0"));

            using VfpTable table = Open(builder, new VfpReadOptions { TrimCharacterFields = false });

            Assert.Equal("ab \0\0\0", RowReader.Single(table)["NAME"]);
        }

        [Fact]
        public void Character_decodes_with_the_header_code_page()
        {
            var builder = new DbfBuilder { LanguageDriver = 0x03 };
            builder.AddField("NAME", 'C', 1);
            builder.AddRecord(false, new byte[] { 0xE9 });

            using VfpTable table = Open(builder);

            Assert.Equal("\u00E9", RowReader.Single(table)["NAME"]);
        }

        [Fact]
        public void Binary_character_field_reads_bytes()
        {
            var builder = new DbfBuilder();
            builder.AddField("BLOB", 'C', 3, binary: true);
            builder.AddRecord(false, new byte[] { 0x00, 0xFF, 0x41 });

            using VfpTable table = Open(builder);

            Assert.Equal(new byte[] { 0x00, 0xFF, 0x41 }, (byte[])RowReader.Single(table)["BLOB"]!);
        }

        [Fact]
        public void Unknown_field_reads_its_raw_bytes()
        {
            var builder = new DbfBuilder();
            builder.AddField("WEIRD", 'Z', 3);
            builder.AddRecord(false, new byte[] { 9, 8, 7 });

            using VfpTable table = Open(builder);

            Assert.Equal(new byte[] { 9, 8, 7 }, (byte[])RowReader.Single(table)["WEIRD"]!);
        }

        [Fact]
        public void Memo_value_reads_null_without_a_memo_file_but_the_row_enumerates()
        {
            var builder = new DbfBuilder { Version = 0x30 };
            builder.AddField("NOTES", 'M', 10);
            builder.AddRecord(false, new byte[10]);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            // Phase 3: a memo column is readable; with no memo stream it is empty (D13).
            Assert.Null(row["NOTES"]);
        }

        [Fact]
        public void Varlength_text_uses_the_last_byte_length_when_the_bit_is_set()
        {
            // _NullFlags width 1 holds NAME's varlength bit (bit 0). The record slot is 10 bytes:
            // "hi" then padding, with the actual length (2) in the last byte.
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 10);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[11];
            payload[0] = (byte)'h';
            payload[1] = (byte)'i';
            for (int i = 2; i < 10; i++)
            {
                payload[i] = (byte)' ';
            }

            payload[10] = 2;  // varlength bit set for NAME
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal("hi", row[0]);
        }

        [Fact]
        public void Varlength_text_fills_the_whole_field_when_the_bit_is_clear()
        {
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 5);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[6];
            Array.Copy(Encoding.ASCII.GetBytes("abc"), payload, 3);
            payload[3] = (byte)' ';
            payload[4] = (byte)' ';
            payload[5] = 0;  // varlength bit clear: the whole field is the value
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal("abc", row[0]);
        }

        [Fact]
        public void Varlength_length_larger_than_the_field_is_clamped()
        {
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 4);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[5];
            Array.Copy(Encoding.ASCII.GetBytes("ab"), payload, 2);
            payload[2] = (byte)' ';
            payload[3] = 200;  // length byte, far larger than the field
            payload[4] = 0x01;  // varlength bit set
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            // The value cannot include the length byte itself, so it is at most width - 1 bytes;
            // trailing padding is then trimmed like any other character value.
            Assert.Equal("ab", row[0]);
        }

        [Fact]
        public void Varbinary_returns_the_varlength_bytes()
        {
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("DATA", 'Q', 8, binary: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[9];
            payload[0] = 0xDE;
            payload[1] = 0xAD;
            payload[2] = 0x01;
            payload[7] = 3;  // DATA's last byte: the value length
            payload[8] = 0x01;  // _NullFlags: DATA's varlength bit
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal(new byte[] { 0xDE, 0xAD, 0x01 }, (byte[])row[0]!);
        }

        [Fact]
        public void Nullable_varlength_field_reads_null_when_its_null_bit_is_set()
        {
            // Bit 0 is the varlength bit, bit 1 the null bit (the documented order). Setting bit 1
            // makes the value null even though the field bytes hold text.
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 5, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[6];
            Array.Copy(Encoding.ASCII.GetBytes("ab"), payload, 2);
            payload[4] = 2;  // varlength length
            payload[5] = 0x02;  // null bit set, varlength bit clear
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Null(row[0]);
        }

        [Fact]
        public void Nullable_fixed_width_field_reads_null_when_its_null_bit_is_set()
        {
            var builder = new DbfBuilder { Version = 0x30 };
            builder.AddField("A", 'C', 4, nullable: true);
            builder.AddField("B", 'N', 3, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[8];
            Array.Copy(Encoding.ASCII.GetBytes("abcd"), payload, 4);
            Array.Copy(Encoding.ASCII.GetBytes(" 12"), 0, payload, 4, 3);
            payload[7] = 0x02;  // null bit for B; A's bytes still look valid
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal("abcd", row["A"]);
            Assert.Null(row["B"]);
        }

        [Fact]
        public void Varlength_and_null_bits_are_allocated_in_field_order()
        {
            // V (varlength 0, null 1), C (null 2), N (null 3).
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("V", 'V', 4, nullable: true);
            builder.AddField("C", 'C', 3, nullable: true);
            builder.AddField("N", 'N', 3, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);
            builder.AddRecord(false, new byte[14]);

            using VfpTable table = Open(builder);

            Assert.Equal(0, table.Schema.Fields[0].VarlengthBit!.Value);
            Assert.Equal(1, table.Schema.Fields[0].NullBit!.Value);
            Assert.Null(table.Schema.Fields[1].VarlengthBit);
            Assert.Equal(2, table.Schema.Fields[1].NullBit!.Value);
            Assert.Null(table.Schema.Fields[2].VarlengthBit);
            Assert.Equal(3, table.Schema.Fields[2].NullBit!.Value);
        }

        [Fact]
        public void Multi_byte_nullflags_read_a_bit_in_the_second_byte()
        {
            // Nine nullable C fields use null bits 0..8. Bit 8 is the low bit of the second
            // _NullFlags byte; setting it nulls only the ninth field.
            var builder = new DbfBuilder { Version = 0x30 };
            for (int i = 0; i < 9; i++)
            {
                builder.AddField("F" + i, 'C', 2, nullable: true);
            }

            builder.AddField("_NullFlags", '0', 2, binary: true, system: true);

            var payload = new byte[(9 * 2) + 2];
            for (int i = 0; i < 9; i++)
            {
                payload[i * 2] = (byte)('a' + i);
                payload[(i * 2) + 1] = (byte)' ';
            }

            payload[19] = 0x01;  // _NullFlags byte 1, bit 0 = null bit 8 = F8
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(((char)('a' + i)).ToString(), row["F" + i]);
            }

            Assert.Null(row["F8"]);
        }

        [Fact]
        public void Non_nullable_field_ignores_a_set_nullflags_byte()
        {
            // Only B is nullable, so the sole bit belongs to B. Every bit set must still not
            // null the non-nullable A: NullBit is only assigned to nullable fields.
            var builder = new DbfBuilder { Version = 0x30 };
            builder.AddField("A", 'C', 4);
            builder.AddField("B", 'C', 4, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[9];
            Array.Copy(Encoding.ASCII.GetBytes("abcd"), 0, payload, 0, 4);
            Array.Copy(Encoding.ASCII.GetBytes("wxyz"), 0, payload, 4, 4);
            payload[8] = 0xFF;  // every bit set
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal("abcd", row["A"]);
            Assert.Null(row["B"]);
        }

        [Fact]
        public void Varlength_clear_keeps_the_last_byte_when_trimming_is_off()
        {
            // With the bit clear the whole field is the value, so the last byte is data even
            // when it is not padding. Trimming off makes a width - 1 truncation observable.
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 5);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[6];
            Array.Copy(Encoding.ASCII.GetBytes("abcdZ"), payload, 5);
            payload[5] = 0x00;  // varlength bit clear
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder, new VfpReadOptions { TrimCharacterFields = false });
            VfpRow row = RowReader.Single(table);

            Assert.Equal("abcdZ", row["NAME"]);
        }

        [Fact]
        public void Nullable_varlength_field_reads_the_value_when_only_the_varlength_bit_is_set()
        {
            // V is nullable: bit 0 is the varlength bit, bit 1 the null bit. Setting bit 0 and
            // clearing bit 1 is the documented "length in the last byte" case.
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 6, nullable: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[7];
            Array.Copy(Encoding.ASCII.GetBytes("hey"), payload, 3);
            payload[5] = 3;  // NAME's last byte: the value length
            payload[6] = 0x01;  // varlength bit set, null bit clear
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal("hey", row["NAME"]);
        }

        [Fact]
        public void Nullable_varbinary_field_reads_the_value_when_only_the_varlength_bit_is_set()
        {
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("DATA", 'Q', 5, nullable: true, binary: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[6];
            payload[0] = 0xAA;
            payload[1] = 0xBB;
            payload[4] = 2;  // DATA's last byte: the value length
            payload[5] = 0x01;  // varlength bit set, null bit clear
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Equal(new byte[] { 0xAA, 0xBB }, (byte[])row["DATA"]!);
        }

        [Fact]
        public void Varchar_with_the_binary_flag_still_decodes_as_text()
        {
            // MSDN lists a "Varchar (binary)" type but restricts flag 0x04 to CHAR/MEMO; the real
            // dbase_32 fixture has V + 0x04 holding ASCII. Pin the D47 behaviour here.
            var builder = new DbfBuilder { Version = 0x32 };
            builder.AddField("NAME", 'V', 6, binary: true);
            builder.AddField("_NullFlags", '0', 1, binary: true, system: true);

            var payload = new byte[7];
            Array.Copy(Encoding.ASCII.GetBytes("hey"), payload, 3);
            payload[5] = 3;
            payload[6] = 0x01;  // varlength bit set
            builder.AddRecord(false, payload);

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.IsType<string>(row["NAME"]);
            Assert.Equal("hey", row["NAME"]);
        }

        [Fact]
        public void Empty_table_yields_no_rows()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);

            using VfpTable table = Open(builder);

            Assert.Empty(table.ReadRows());
        }

        [Fact]
        public void Truncated_record_names_the_record()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("abcd"));
            byte[] bytes = builder.Build();

            // Cut the single record in half; the header still claims one record.
            var truncated = new byte[bytes.Length - 2];
            Array.Copy(bytes, truncated, truncated.Length);

            using VfpTable table = VfpTable.Open(new MemoryStream(truncated));

            VfpFormatException ex = Assert.Throws<VfpFormatException>(
                () => table.ReadRows().ToList());
            Assert.Contains("record 1", ex.Message);
        }

        [Fact]
        public void Non_seekable_stream_can_be_enumerated_once()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using var stream = new NonSeekableStream(builder.Build());
            using VfpTable table = VfpTable.Open(stream);

            Assert.Equal("live", RowReader.Single(table)["NAME"]);
            Assert.Throws<InvalidOperationException>(() => table.ReadRows().ToList());
        }

        [Fact]
        public void Indexer_by_name_is_case_insensitive()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder);

            Assert.Equal("live", RowReader.Single(table)["name"]);
        }

        [Fact]
        public void Indexer_rejects_a_bad_index()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Throws<ArgumentOutOfRangeException>(() => row[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => row[row.FieldCount]);
        }

        [Fact]
        public void Indexer_rejects_an_unknown_name()
        {
            var builder = new DbfBuilder();
            builder.AddField("NAME", 'C', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("live"));

            using VfpTable table = Open(builder);
            VfpRow row = RowReader.Single(table);

            Assert.Throws<KeyNotFoundException>(() => row["MISSING"]);
        }

        private static byte[] DateParts(int julianDay, int milliseconds)
        {
            var bytes = new List<byte>();
            bytes.AddRange(ByteOrder.LittleEndianInt32(julianDay));
            bytes.AddRange(ByteOrder.LittleEndianInt32(milliseconds));
            return bytes.ToArray();
        }

        /// <summary>A read-only stream that refuses to seek, so the reader's single-pass path runs.</summary>
        private sealed class NonSeekableStream : Stream
        {
            private readonly byte[] _bytes;
            private int _position;

            internal NonSeekableStream(byte[] bytes)
            {
                _bytes = bytes;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { return _position; }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int available = Math.Min(count, _bytes.Length - _position);
                if (available <= 0)
                {
                    return 0;
                }

                Array.Copy(_bytes, _position, buffer, offset, available);
                _position += available;
                return available;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override void Flush()
            {
            }
        }
    }
}

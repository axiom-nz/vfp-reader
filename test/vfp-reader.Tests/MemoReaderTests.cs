using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VfpReader.Tests.Fixtures;
using Xunit;

namespace VfpReader.Tests
{
    /// <summary>
    /// Unit tests for the three memo-file layouts the reader understands: the dBASE III <c>.dbt</c>
    /// (version <c>0x83</c>), the dBASE IV <c>.dbt</c> (version <c>0x8b</c>) and the FoxPro /
    /// Visual FoxPro <c>.fpt</c> (versions <c>0x30</c> / <c>0x31</c> / <c>0xf5</c>). The memo bytes
    /// are written by hand, never by the reader, and the real fixtures are checked again in the
    /// golden tests.
    /// </summary>
    public class MemoReaderTests
    {
        private static VfpTable Open(byte[] dbf, byte[]? memo)
        {
            return VfpTable.Open(new MemoryStream(dbf), memo is null ? null : new MemoryStream(memo));
        }

        // ---- dBASE III (.dbt, 0x83): 512-byte blocks, 0x1A-terminated, NUL-padded ----

        [Fact]
        public void Dbase3_reads_text_from_block_one()
        {
            byte[] memo = Dbase3Blocks(Text("hello world"));
            byte[] dbf = MemoTable(0x83, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.IsType<string>(row["MEMO"]);
            Assert.Equal("hello world", row["MEMO"]);
        }

        [Fact]
        public void Dbase3_spans_blocks_and_strips_nuls_and_eof_bytes()
        {
            // 600 bytes of 'x' across two 512-byte blocks, padded and terminated.
            string text = new string('x', 600);
            byte[] memo = Dbase3Blocks(Text(text));
            byte[] dbf = MemoTable(0x83, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            string? value = (string?)row["MEMO"];
            Assert.NotNull(value);
            Assert.Equal(600, value!.Length);
            Assert.DoesNotContain('\0', value);
        }

        [Theory]
        [InlineData("         0")]
        [InlineData("          ")]
        [InlineData("not a ptr!")]
        public void Dbase3_blank_or_invalid_pointer_reads_as_null(string pointer)
        {
            byte[] dbf = MemoTable(0x83, 'M', 10, Encoding.ASCII.GetBytes(pointer));

            using VfpTable table = Open(dbf, Dbase3Blocks(Text("unused")));
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void Dbase3_pointer_past_the_end_reads_as_empty_not_throws()
        {
            byte[] dbf = MemoTable(0x83, 'M', 10, AsciiPointer(99));

            using VfpTable table = Open(dbf, Dbase3Blocks(Text("short")));
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        // ---- dBASE IV (.dbt, 0x8b): 8-byte header with a little-endian length ----

        [Fact]
        public void Dbase4_reads_the_length_prefixed_payload()
        {
            byte[] memo = Dbase4Blocks(Encoding.ASCII.GetBytes("First memo\r\n"));
            byte[] dbf = MemoTable(0x8b, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Equal("First memo\r\n", row["MEMO"]);
        }

        [Fact]
        public void Dbase4_truncated_header_reads_as_null()
        {
            byte[] dbf = MemoTable(0x8b, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, new byte[] { 0xFF, 0xFF, 0x08, 0x00 });
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void Dbase4_length_past_eof_is_clamped()
        {
            // A header claims 0x7FFFFFFF bytes; only "abc" follows.
            byte[] memo = Dbase4Bytes(0x7FFFFFFF, Encoding.ASCII.GetBytes("abc"));
            byte[] dbf = MemoTable(0x8b, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Equal("abc", row["MEMO"]);
        }

        [Fact]
        public void Dbase4_zero_length_reads_as_null()
        {
            byte[] memo = Dbase4Bytes(0, Array.Empty<byte>());
            byte[] dbf = MemoTable(0x8b, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        // ---- FoxPro / Visual FoxPro (.fpt): big-endian type and length ----

        [Fact]
        public void FoxPro_reads_text_with_a_binary_little_endian_pointer()
        {
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Text("Domestic Life\r\nWeddings\r\n") });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.IsType<string>(row["MEMO"]);
            Assert.Equal("Domestic Life\r\nWeddings\r\n", row["MEMO"]);
        }

        [Fact]
        public void FoxPro_reads_a_non_default_block_size_from_the_header()
        {
            byte[] memo = Fpt(blockSize: 512, entries: new[] { FptEntry.Text("bigger blocks") });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Equal("bigger blocks", row["MEMO"]);
        }

        [Fact]
        public void FoxPro_content_larger_than_one_block_spans_blocks()
        {
            string text = new string('y', 200);
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Text(text) });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Equal(text, row["MEMO"]);
        }

        [Fact]
        public void FoxPro_zero_pointer_reads_as_null()
        {
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Text("unused") });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(0));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void FoxPro_non_text_block_reads_as_null()
        {
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Binary(new byte[] { 1, 2, 3 }) });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void FoxPro_zero_size_block_reads_as_null()
        {
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Empty() });
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void FoxPro_truncated_header_reads_as_null()
        {
            byte[] dbf = MemoTable(0x30, 'M', 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, new byte[] { 0, 0, 2, 0xDA, 0, 0 });
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        // ---- Binary memo types (G / P / W) ----

        [Theory]
        [InlineData('G')]
        [InlineData('P')]
        [InlineData('W')]
        public void Binary_memo_types_return_raw_bytes(char type)
        {
            byte[] payload = { 0x00, 0x01, 0xFE, 0xFF };
            byte[] memo = Fpt(blockSize: 64, entries: new[] { FptEntry.Text(payload) });
            byte[] dbf = MemoTable(0x30, type, 4, LittleEndianPointer(1));

            using VfpTable table = Open(dbf, memo);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.IsType<byte[]>(row["MEMO"]);
            Assert.Equal(payload, (byte[])row["MEMO"]!);
        }

        // ---- Discovery, missing files and the Phase 4 guard ----

        [Fact]
        public void Missing_memo_file_reads_memo_columns_as_null()
        {
            byte[] dbf = MemoTable(0x83, 'M', 10, AsciiPointer(1));

            using VfpTable table = Open(dbf, null);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Null(row["MEMO"]);
        }

        [Fact]
        public void Varlength_still_throws_after_memo_landed()
        {
            byte[] dbf = MemoTable(0x30, 'V', 4, new byte[] { 0, 0, 0, 0 });

            using VfpTable table = Open(dbf, null);
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Throws<NotSupportedException>(() => row["MEMO"]);
        }

        [Fact]
        public void Table_without_memo_columns_still_opens_beside_a_memo_file()
        {
            var builder = new DbfBuilder();
            builder.AddField("NUM", 'N', 4);
            builder.AddRecord(false, Encoding.ASCII.GetBytes("  42"));

            using VfpTable table = VfpTable.Open(new MemoryStream(builder.Build()), new MemoryStream(Dbase3Blocks(Text("x"))));
            VfpRow row = Assert.Single(table.ReadRows());

            Assert.Equal(42m, row["NUM"]);
        }

        [Fact]
        public void Explicit_memo_path_is_used()
        {
            // The table's sibling .dbt is named after the table; MemoPath points elsewhere.
            string directory = CreateTempDirectory();
            try
            {
                string tablePath = Path.Combine(directory, "table.dbf");
                File.WriteAllBytes(tablePath, MemoTable(0x83, 'M', 10, AsciiPointer(1)));

                string memoPath = Path.Combine(directory, "renamed.dbt");
                File.WriteAllBytes(memoPath, Dbase3Blocks(Text("from another file")));

                using VfpTable table = VfpTable.Open(tablePath, new VfpReadOptions { MemoPath = memoPath });
                VfpRow row = Assert.Single(table.ReadRows());

                Assert.Equal("from another file", row["MEMO"]);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        // ---- Real fixture integration ----

        [Fact]
        public void Dbase_30_fixture_reads_a_real_foxpro_memo()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_30");
            using VfpTable table = fixture.Open();
            VfpRow row = table.ReadRows().First();

            Assert.Equal("Domestic Life\r\nWeddings\r\n", row["CLASSES"]);
        }

        [Fact]
        public void Dbase_83_fixture_reads_a_real_dbase3_memo()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_83");
            using VfpTable table = fixture.Open();
            VfpRow row = table.ReadRows().First();

            string? value = (string?)row["DESC"];
            Assert.NotNull(value);
            Assert.StartsWith("Our Original assortment", value);
            Assert.Contains("Raspberry Blanc.", value);
        }

        [Fact]
        public void Missing_memo_fixture_reads_desc_as_null()
        {
            GoldenFixture fixture = GoldenManifest.Find("dbase_83_missing_memo");
            using VfpTable table = fixture.Open();
            VfpRow row = table.ReadRows().First();

            Assert.Null(row["DESC"]);
        }

        // ---- Builders ----

        /// <summary>A one-field table whose pointer bytes are supplied verbatim.</summary>
        private static byte[] MemoTable(byte version, char type, byte length, byte[] pointer)
        {
            var builder = new DbfBuilder { Version = version, HasMemoFlag = true };
            builder.AddField("MEMO", type, length);
            builder.AddRecord(false, pointer);
            return builder.Build();
        }

        private static byte[] AsciiPointer(int block)
        {
            return Encoding.ASCII.GetBytes(block.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(10));
        }

        private static byte[] LittleEndianPointer(int block)
        {
            return new[] { (byte)block, (byte)(block >> 8), (byte)(block >> 16), (byte)(block >> 24) };
        }

        private static byte[] Text(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        /// <summary>Builds a dBASE III memo file: 512-byte blocks with the text in block 1.</summary>
        private static byte[] Dbase3Blocks(byte[] text)
        {
            var content = new List<byte>(new byte[512]);
            content.AddRange(text);
            content.Add(0x1A);
            while (content.Count % 512 != 0)
            {
                content.Add(0);
            }

            content.AddRange(new byte[512]);
            return content.ToArray();
        }

        /// <summary>Builds a dBASE IV memo file with one block: 8-byte header then the payload.</summary>
        private static byte[] Dbase4Blocks(byte[] payload)
        {
            return Dbase4Bytes(payload.Length, payload);
        }

        private static byte[] Dbase4Bytes(uint length, byte[] payload)
        {
            var bytes = new List<byte> { 0xFF, 0xFF, 0x08, 0x00 };
            bytes.AddRange(BitConverter.GetBytes(length));
            bytes.AddRange(payload);
            bytes.AddRange(new byte[8]);
            return bytes.ToArray();
        }

        /// <summary>
        /// Builds an FPT file whose header is exactly one block (so the first entry is block 1),
        /// then one block per entry. The real format pads the header to 512 bytes and starts at
        /// <c>512 / blockSize</c>; only bytes 6-7 and the block arithmetic matter to the reader, so
        /// this keeps the test pointers simple and still exercises a non-512 block size.
        /// </summary>
        private static byte[] Fpt(int blockSize, FptEntry[] entries)
        {
            var bytes = new List<byte>();
            bytes.AddRange(new byte[6]);
            bytes.Add((byte)(blockSize >> 8));
            bytes.Add((byte)blockSize);
            while (bytes.Count < blockSize)
            {
                bytes.Add(0);
            }

            foreach (FptEntry entry in entries)
            {
                byte[] block = entry.Build(blockSize);
                bytes.AddRange(block);
            }

            return bytes.ToArray();
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "vfp-memo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>One FPT block: big-endian type, big-endian length, then padded payload.</summary>
        private sealed class FptEntry
        {
            private readonly uint _type;
            private readonly byte[] _payload;

            private FptEntry(uint type, byte[] payload)
            {
                _type = type;
                _payload = payload;
            }

            internal static FptEntry Text(string text)
            {
                return new FptEntry(1, Encoding.ASCII.GetBytes(text));
            }

            internal static FptEntry Text(byte[] bytes)
            {
                return new FptEntry(1, bytes);
            }

            internal static FptEntry Binary(byte[] bytes)
            {
                return new FptEntry(2, bytes);
            }

            internal static FptEntry Empty()
            {
                return new FptEntry(1, Array.Empty<byte>());
            }

            internal byte[] Build(int blockSize)
            {
                var bytes = new List<byte>();
                bytes.AddRange(BigEndian(_type));
                bytes.AddRange(BigEndian((uint)_payload.Length));
                bytes.AddRange(_payload);
                while (bytes.Count % blockSize != 0)
                {
                    bytes.Add(0);
                }

                return bytes.ToArray();
            }

            private static byte[] BigEndian(uint value)
            {
                return new[]
                {
                    (byte)(value >> 24),
                    (byte)(value >> 16),
                    (byte)(value >> 8),
                    (byte)value,
                };
            }
        }
    }
}

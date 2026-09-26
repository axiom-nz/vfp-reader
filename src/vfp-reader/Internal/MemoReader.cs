using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;

namespace VfpReader.Internal
{
    /// <summary>
    /// Reads memo payloads from an <c>.fpt</c> (FoxPro / Visual FoxPro) or <c>.dbt</c> (dBASE III
    /// and dBASE IV) file. The format is chosen once from the table version; every access seeks, so
    /// values can be read in any order and long after the table is opened. The reader never owns or
    /// disposes the stream — <see cref="VfpTable"/> does.
    /// </summary>
    internal sealed class MemoReader
    {
        /// <summary>dBASE III and IV memo block size; both formats use fixed 512-byte blocks.</summary>
        private const int DbaseBlockSize = 512;

        /// <summary>Bytes in an FPT block header, and in a dBASE IV block header.</summary>
        private const int BlockHeaderSize = 8;

        /// <summary>The block size FoxPro falls back to when its header cannot be read.</summary>
        private const int DefaultFoxProBlockSize = 64;

        private readonly Stream _stream;
        private readonly MemoFormat _format;
        private readonly bool _pointerIsBinary;
        private int _foxProBlockSize;

        private MemoReader(Stream stream, MemoFormat format, bool pointerIsBinary)
        {
            _stream = stream;
            _format = format;
            _pointerIsBinary = pointerIsBinary;
        }

        /// <summary>
        /// Creates a reader for the memo file that belongs to a table with <paramref name="version"/>.
        /// Visual FoxPro, FoxPro and FoxPro 2.x tables use the FPT layout; dBASE III uses the
        /// dBASE III DBT layout; every other version falls back to the dBASE IV DBT layout, the
        /// same dispatch the reference implementation uses.
        /// </summary>
        public static MemoReader ForVersion(Stream stream, byte version)
        {
            bool visualFoxPro = VfpVersion.IsVisualFoxPro(version);
            bool foxPro = visualFoxPro
                || version == VfpVersion.FoxProWithMemo
                || version == VfpVersion.FoxBasePlusWithMemo;

            MemoFormat format;
            if (foxPro)
            {
                format = MemoFormat.FoxPro;
            }
            else if (version == VfpVersion.Dbase3WithMemo)
            {
                format = MemoFormat.Dbase3;
            }
            else
            {
                format = MemoFormat.Dbase4;
            }

            // Visual FoxPro stores the block number as a little-endian integer; dBASE and
            // FoxPro 2.x store it as ASCII digits.
            return new MemoReader(stream, format, pointerIsBinary: visualFoxPro);
        }

        /// <summary>
        /// Decodes a memo field's fixed-width block pointer from a record. Visual FoxPro fields
        /// hold a four-byte little-endian integer; the other formats hold ASCII decimal digits, so
        /// a blank or unparseable pointer is zero. Header byte 29's language driver never changes
        /// this: the pointer form follows the table version.
        /// </summary>
        public long Pointer(byte[] record, int offset, int width)
        {
            if (_pointerIsBinary)
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(record.AsSpan(offset));
            }

            string text = Encoding.ASCII.GetString(record, offset, width).Trim();
            return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long block)
                ? block
                : 0L;
        }

        /// <summary>
        /// Reads the raw memo payload at <paramref name="block"/>, or <c>null</c> when there is no
        /// value: a zero or negative pointer, a short block header, a non-text FPT block, a length
        /// past the end of the file, or a stream that cannot be seeked. Memo quirks read as empty
        /// rather than throwing; a missing memo file is handled by the caller.
        /// </summary>
        public byte[]? Read(long block)
        {
            if (block <= 0)
            {
                return null;
            }

            switch (_format)
            {
                case MemoFormat.FoxPro:
                    return ReadFoxPro(block);

                case MemoFormat.Dbase3:
                    return ReadDbase3(block);

                default:
                    return ReadDbase4(block);
            }
        }

        /// <summary>
        /// dBASE III memo: fixed 512-byte blocks. The text is terminated by <c>0x1A</c> and padded
        /// with NULs, and both are removed from every block. A block shorter than the block size
        /// (including a missing one) ends the content, so a pointer past the end of the file reads
        /// as the partial text rather than failing.
        /// </summary>
        private byte[]? ReadDbase3(long block)
        {
            if (!TrySeek(block * DbaseBlockSize))
            {
                return null;
            }

            using (var content = new MemoryStream())
            {
                var buffer = new byte[DbaseBlockSize];
                while (true)
                {
                    int read = ReadUpTo(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        break;
                    }

                    // dBASE III terminates the text with 0x1A and pads with NULs. The reference
                    // strips both and then stops as soon as the *stripped* block is shorter than
                    // a full block, so the terminator (or a short final read) ends the content.
                    int kept = 0;
                    for (int i = 0; i < read; i++)
                    {
                        byte value = buffer[i];
                        if (value != 0x00 && value != 0x1A)
                        {
                            content.WriteByte(value);
                            kept++;
                        }
                    }

                    if (kept < buffer.Length)
                    {
                        break;
                    }
                }

                return content.Length == 0 ? null : content.ToArray();
            }
        }

        /// <summary>
        /// dBASE IV memo: an eight-byte block header whose little-endian length at offset 4 bounds
        /// the text that follows. The length is clamped to the bytes remaining so a crafted value
        /// cannot force a huge allocation.
        /// </summary>
        private byte[]? ReadDbase4(long block)
        {
            if (!TrySeek(block * DbaseBlockSize))
            {
                return null;
            }

            byte[]? header = ReadBlockHeader();
            if (header is null)
            {
                return null;
            }

            long length = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));

            return ReadPayload(length);
        }

        /// <summary>
        /// FoxPro / Visual FoxPro FPT: the block begins with a big-endian type and a big-endian
        /// length. Only a type of 1 (text) is a value; other types belong to general or picture
        /// fields and read as empty here. The length may span blocks.
        /// </summary>
        private byte[]? ReadFoxPro(long block)
        {
            if (!TrySeek(block * FoxProBlockSize()))
            {
                return null;
            }

            byte[]? header = ReadBlockHeader();
            if (header is null)
            {
                return null;
            }

            uint type = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(0));
            uint size = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
            if (type != 1 || size == 0)
            {
                return null;
            }

            return ReadPayload(size);
        }

        /// <summary>Reads <paramref name="length"/> bytes after clamping to what remains.</summary>
        private byte[]? ReadPayload(long length)
        {
            if (_stream.CanSeek)
            {
                long remaining = _stream.Length - _stream.Position;
                if (length > remaining)
                {
                    length = remaining;
                }
            }

            if (length <= 0 || length > int.MaxValue)
            {
                return null;
            }

            var content = new byte[(int)length];
            return ReadUpTo(content, 0, content.Length) == content.Length ? content : null;
        }

        /// <summary>Reads the eight-byte block header, or null when it is truncated.</summary>
        private byte[]? ReadBlockHeader()
        {
            var header = new byte[BlockHeaderSize];
            return ReadUpTo(header, 0, header.Length) == header.Length ? header : null;
        }

        private int FoxProBlockSize()
        {
            if (_foxProBlockSize > 0)
            {
                return _foxProBlockSize;
            }

            long saved = _stream.CanSeek ? _stream.Position : 0;
            if (TrySeek(0))
            {
                var header = new byte[BlockHeaderSize];
                if (ReadUpTo(header, 0, header.Length) == header.Length)
                {
                    _foxProBlockSize = (header[6] << 8) | header[7];
                }
            }

            if (_stream.CanSeek)
            {
                _stream.Position = saved;
            }

            return _foxProBlockSize > 0 ? _foxProBlockSize : DefaultFoxProBlockSize;
        }

        private bool TrySeek(long position)
        {
            if (!_stream.CanSeek || position < 0)
            {
                return false;
            }

            try
            {
                _stream.Position = position;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads up to <paramref name="count"/> bytes, stopping early at the end of the stream or
        /// on an I/O failure. Never throws and never returns a negative count, so callers can treat
        /// a short read as missing data.
        /// </summary>
        private int ReadUpTo(byte[] buffer, int offset, int count)
        {
            return StreamFill.ReadAtMost(_stream, buffer, offset, count);
        }

        private enum MemoFormat
        {
            FoxPro,
            Dbase3,
            Dbase4,
        }
    }
}

using System;
using System.Buffers.Binary;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// The one place the tests write little- and big-endian integers. Hand-rolled shift chains were
    /// repeated across <c>DbfBuilder</c>, <c>RecordReaderTests</c>, <c>MemoReaderTests</c> and
    /// <c>HeaderTests</c>; tests run on <c>net9.0</c>, so <c>BinaryPrimitives</c> is available and
    /// mirrors exactly what the library reads.
    /// </summary>
    internal static class ByteOrder
    {
        internal static byte[] LittleEndianUInt16(ushort value)
        {
            var bytes = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
            return bytes;
        }

        internal static byte[] LittleEndianUInt32(uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return bytes;
        }

        internal static byte[] LittleEndianInt32(int value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            return bytes;
        }

        internal static byte[] LittleEndianInt64(long value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            return bytes;
        }

        internal static byte[] BigEndianUInt32(uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            return bytes;
        }

        internal static void WriteUInt16LittleEndian(byte[] buffer, int offset, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset), value);
        }

        internal static void WriteUInt32LittleEndian(byte[] buffer, int offset, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), value);
        }
    }
}

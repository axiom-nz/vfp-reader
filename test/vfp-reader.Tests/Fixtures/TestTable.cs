using System.IO;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// The shared in-memory openers. <c>HeaderTests</c>, <c>RecordReaderTests</c> and
    /// <c>MemoReaderTests</c> each had their own <c>VfpTable.Open(new MemoryStream(…))</c> wrapper;
    /// the call sites now share these.
    /// </summary>
    internal static class TestTable
    {
        /// <summary>Opens a built table over an in-memory stream.</summary>
        internal static VfpTable Open(DbfBuilder builder, VfpReadOptions? options = null)
        {
            return VfpTable.Open(new MemoryStream(builder.Build()), options: options);
        }

        /// <summary>Opens raw table bytes, optionally with a memo stream and options.</summary>
        internal static VfpTable Open(byte[] dbf, byte[]? memo = null, VfpReadOptions? options = null)
        {
            return VfpTable.Open(
                new MemoryStream(dbf),
                memo is null ? null : new MemoryStream(memo),
                options);
        }
    }
}

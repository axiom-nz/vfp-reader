using System;
using System.Globalization;
using System.IO;
using System.Text;
using VfpReader.Internal;

namespace VfpReader
{
    /// <summary>
    /// A read-only view of one DBF table. Opening it reads and validates the header; records are
    /// streamed later, one at a time, without loading the table.
    /// </summary>
    public sealed class VfpTable : IDisposable
    {
        private readonly Stream _dbf;
        private readonly Stream? _memo;
        private readonly bool _ownsDbf;
        private readonly bool _ownsMemo;
        private readonly VfpReadOptions _options;
        private readonly VfpHeader _header;
        private bool _disposed;

        private VfpTable(
            Stream dbf,
            Stream? memo,
            bool ownsDbf,
            bool ownsMemo,
            VfpReadOptions options,
            VfpHeader header,
            Encoding encoding)
        {
            _dbf = dbf;
            _memo = memo;
            _ownsDbf = ownsDbf;
            _ownsMemo = ownsMemo;
            _options = options;
            _header = header;
            Encoding = encoding;
            Schema = BuildSchema(header);
        }

        /// <summary>The table's schema, known from the header alone.</summary>
        public VfpSchema Schema { get; }

        internal VfpHeader Header
        {
            get { return _header; }
        }

        internal Encoding Encoding { get; }

        internal VfpReadOptions Options
        {
            get { return _options; }
        }

        internal Stream VfpStream
        {
            get { return _dbf; }
        }

        internal Stream? MemoStream
        {
            get { return _memo; }
        }

        /// <summary>
        /// Opens the table at <paramref name="path"/> and reads its header. The table keeps the
        /// file open until it is disposed.
        /// </summary>
        public static VfpTable Open(string path, VfpReadOptions? options = null)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            options ??= new VfpReadOptions();
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                return OpenCore(stream, null, ownsDbf: true, ownsMemo: false, options, path);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Opens a table over caller-provided streams. The streams are left open; the caller owns
        /// them.
        /// </summary>
        public static VfpTable Open(Stream dbf, Stream? memo = null, VfpReadOptions? options = null)
        {
            if (dbf is null)
            {
                throw new ArgumentNullException(nameof(dbf));
            }

            options ??= new VfpReadOptions();
            return OpenCore(dbf, memo, ownsDbf: false, ownsMemo: false, options, null);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_ownsMemo)
            {
                _memo?.Dispose();
            }

            if (_ownsDbf)
            {
                _dbf.Dispose();
            }
        }

        private static VfpTable OpenCore(
            Stream dbf,
            Stream? memo,
            bool ownsDbf,
            bool ownsMemo,
            VfpReadOptions options,
            string? path)
        {
            if (!dbf.CanRead)
            {
                throw new ArgumentException("The stream must be readable.", nameof(dbf));
            }

            byte[] headerBytes = ReadHeader(dbf, path);
            int headerLength = headerBytes.Length;

            CodePage codePage;
            Encoding encoding;
            if (options.Encoding is not null)
            {
                encoding = options.Encoding;
                codePage = (CodePage)encoding.CodePage;
            }
            else
            {
                codePage = CodePageMap.FromLanguageDriver(headerBytes[29]) ?? CodePageMap.DefaultCodePage;
                encoding = ResolveEncoding(codePage, path);
            }

            VfpHeader header = HeaderParser.Parse(headerBytes, headerLength, encoding, codePage, path);
            return new VfpTable(dbf, memo, ownsDbf, ownsMemo, options, header, encoding);
        }

        private static byte[] ReadHeader(Stream dbf, string? path)
        {
            var prefix = new byte[HeaderParser.HeaderSize];
            ReadFully(dbf, prefix, 0, prefix.Length, path);

            int headerLength = prefix[8] | (prefix[9] << 8);
            if (headerLength < HeaderParser.HeaderSize + 1)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "header length {0} is too small for any field", headerLength),
                    path,
                    8);
            }

            if (dbf.CanSeek && headerLength - HeaderParser.HeaderSize > dbf.Length - dbf.Position)
            {
                throw new VfpFormatException(
                    string.Format(CultureInfo.InvariantCulture, "header length {0} runs past the end of the file", headerLength),
                    path,
                    8);
            }

            var header = new byte[headerLength];
            Buffer.BlockCopy(prefix, 0, header, 0, HeaderParser.HeaderSize);
            ReadFully(dbf, header, HeaderParser.HeaderSize, headerLength - HeaderParser.HeaderSize, path);
            return header;
        }

        private static void ReadFully(Stream stream, byte[] buffer, int offset, int count, string? path)
        {
            int read = 0;
            while (read < count)
            {
                int n;
                try
                {
                    n = stream.Read(buffer, offset + read, count - read);
                }
                catch (IOException ex)
                {
                    throw new VfpFormatException(
                        "failed while reading the table header",
                        path,
                        offset + read,
                        ex);
                }

                if (n <= 0)
                {
                    throw new VfpFormatException(
                        "unexpected end of file while reading the table header",
                        path,
                        offset + read);
                }

                read += n;
            }
        }

        private static Encoding ResolveEncoding(CodePage codePage, string? path)
        {
            try
            {
                return Encoding.GetEncoding((int)codePage);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                throw new VfpFormatException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Code page {0} is not available. On .NET Core, register the provider once at startup "
                        + "(Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);) and reference the "
                        + "System.Text.Encoding.CodePages package.",
                        (int)codePage),
                    path,
                    null,
                    ex);
            }
        }

        private static VfpSchema BuildSchema(VfpHeader header)
        {
            return new VfpSchema(
                header.Version,
                header.RecordCount,
                header.HeaderLength,
                header.RecordLength,
                header.LastUpdate,
                header.CodePage,
                header.DatabasePath,
                header.HasIndex,
                header.HasMemo,
                header.IsDatabase,
                header.Fields);
        }
    }
}

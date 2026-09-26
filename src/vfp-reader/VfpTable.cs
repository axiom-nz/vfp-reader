using System;
using System.Collections.Generic;
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
        private readonly MemoReader? _memoReader;
        private readonly bool _ownsDbf;
        private readonly bool _ownsMemo;
        private readonly VfpReadOptions _options;
        private readonly VfpHeader _header;
        private readonly string? _path;
        private bool _disposed;
        private bool _recordsConsumed;

        private VfpTable(
            Stream dbf,
            Stream? memo,
            MemoReader? memoReader,
            bool ownsDbf,
            bool ownsMemo,
            VfpReadOptions options,
            VfpHeader header,
            Encoding encoding,
            string? path)
        {
            _dbf = dbf;
            _memo = memo;
            _memoReader = memoReader;
            _ownsDbf = ownsDbf;
            _ownsMemo = ownsMemo;
            _options = options;
            _header = header;
            Encoding = encoding;
            _path = path;
            Schema = new VfpSchema(header);
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
            Stream stream = Storage.Current.OpenRead(path);
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

        /// <summary>
        /// Streams the table's records, one decoded row at a time, without loading the table.
        /// Deleted records are skipped unless <see cref="VfpReadOptions.IncludeDeleted"/> is set.
        /// The reader is forward-only and not thread-safe: enumerate one sequence at a time. On a
        /// seekable stream every call restarts at the first record; a non-seekable stream can be
        /// read once (a second enumeration throws <see cref="InvalidOperationException"/>).
        /// </summary>
        public IEnumerable<VfpRow> ReadRows()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(VfpTable));
            }

            return ReadRowsCore();
        }

        private IEnumerable<VfpRow> ReadRowsCore()
        {
            VfpHeader header = _header;
            if (_dbf.CanSeek)
            {
                _dbf.Position = header.HeaderLength;
            }
            else if (_recordsConsumed)
            {
                throw new InvalidOperationException(
                    "This table's stream is not seekable, so its records can only be enumerated once.");
            }
            else
            {
                _recordsConsumed = true;
            }

            for (long number = 1; number <= header.RecordCount; number++)
            {
                var record = new byte[header.RecordLength];
                StreamFill.ReadFully(
                    _dbf,
                    record,
                    0,
                    record.Length,
                    _path,
                    "record " + number.ToString(CultureInfo.InvariantCulture));

                bool deleted = record[0] == (byte)'*';
                if (deleted && !_options.IncludeDeleted)
                {
                    continue;
                }

                yield return new VfpRow(header, Encoding, record, number, _options.TrimCharacterFields, _memoReader);
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

            // A sibling memo file is only worth opening when the header says the table has one.
            // The caller's explicit path wins; otherwise the .fpt / .dbt next to the table is
            // probed case-insensitively.
            if (memo is null && path is not null && header.HasMemo)
            {
                string? memoPath = ResolveMemoPath(path, options);
                if (memoPath is not null)
                {
                    Stream opened = Storage.Current.OpenRead(memoPath);
                    try
                    {
                        MemoReader? ownedReader = MemoReader.ForVersion(opened, header.Version);
                        return new VfpTable(dbf, opened, ownedReader, ownsDbf, ownsMemo: true, options, header, encoding, path);
                    }
                    catch
                    {
                        opened.Dispose();
                        throw;
                    }
                }
            }

            MemoReader? memoReader = memo is null ? null : MemoReader.ForVersion(memo, header.Version);
            return new VfpTable(dbf, memo, memoReader, ownsDbf, ownsMemo, options, header, encoding, path);
        }

        /// <summary>
        /// Finds the memo file that sits beside <paramref name="tablePath"/>. An explicit
        /// <see cref="VfpReadOptions.MemoPath"/> wins; otherwise the sibling whose name is the
        /// table's base name plus <c>.fpt</c> or <c>.dbt</c> is used, in any case.
        /// </summary>
        private static string? ResolveMemoPath(string tablePath, VfpReadOptions options)
        {
            if (!string.IsNullOrEmpty(options.MemoPath))
            {
                return options.MemoPath;
            }

            string? directory = Path.GetDirectoryName(tablePath);
            if (string.IsNullOrEmpty(directory))
            {
                directory = ".";
            }

            string baseName = Path.GetFileNameWithoutExtension(tablePath);
            string[] extensions = { ".fpt", ".FPT", ".dbt", ".DBT" };
            foreach (string extension in extensions)
            {
                string candidate = Path.Combine(directory, baseName + extension);
                if (Storage.Current.FileExists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static byte[] ReadHeader(Stream dbf, string? path)
        {
            var prefix = new byte[HeaderParser.HeaderSize];
            StreamFill.ReadFully(dbf, prefix, 0, prefix.Length, path, "the table header");

            int headerLength = prefix[8] | (prefix[9] << 8);
            if (headerLength < HeaderParser.MinimumHeaderLength)
            {
                throw HeaderParser.HeaderTooSmall(headerLength, path);
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
            StreamFill.ReadFully(dbf, header, HeaderParser.HeaderSize, headerLength - HeaderParser.HeaderSize, path, "the table header");
            return header;
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
    }
}

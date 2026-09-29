using System;
using System.Collections.Generic;
using System.IO;
using VfpReader.Internal;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// An in-memory <see cref="IStorage"/>. Paths are matched case-insensitively so the fake can
    /// stand in for Windows and the reader's case-insensitive sibling probe.
    /// </summary>
    internal sealed class FakeStorage : IStorage
    {
        private readonly Dictionary<string, byte[]> _files =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The bytes the fake reports for a path.</summary>
        internal Dictionary<string, byte[]> Files
        {
            get { return _files; }
        }

        public bool FileExists(string path)
        {
            return _files.ContainsKey(Normalize(path));
        }

        public Stream OpenRead(string path)
        {
            if (!_files.TryGetValue(Normalize(path), out byte[]? bytes))
            {
                throw new FileNotFoundException("The fake storage has no such file.", path);
            }

            return new MemoryStream(bytes, writable: false);
        }

        /// <summary>
        /// Folds Windows and Unix directory separators together so the fake matches the same
        /// logical file on every platform: the reader builds sibling paths with
        /// <see cref="Path.Combine(string, string)"/>, which emits a backslash on Windows.
        /// </summary>
        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }

    /// <summary>Swaps <see cref="Storage.Current"/> for the fake and restores it on dispose.</summary>
    internal sealed class StorageScope : IDisposable
    {
        private readonly IStorage _previous;
        private bool _disposed;

        internal StorageScope(IStorage storage)
        {
            _previous = Storage.Current;
            Storage.Current = storage;
        }

        /// <summary>Installs <paramref name="storage"/> for the lifetime of the returned scope.</summary>
        internal static StorageScope Use(IStorage storage)
        {
            return new StorageScope(storage);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Storage.Current = _previous;
        }
    }
}

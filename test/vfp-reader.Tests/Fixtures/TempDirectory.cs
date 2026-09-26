using System;
using System.IO;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// A temp directory that deletes itself (recursively) on dispose. Replaces the two hand-rolled
    /// try/finally lifecycles in <c>MemoReaderTests</c> and <c>HeaderTests</c>.
    /// </summary>
    internal sealed class TempDirectory : IDisposable
    {
        private bool _disposed;

        internal TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "vfp-reader-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        /// <summary>The directory path; it exists until the instance is disposed.</summary>
        internal string Path { get; }

        /// <summary>Combines a file name with <see cref="Path"/>.</summary>
        internal string File(string name)
        {
            return System.IO.Path.Combine(Path, name);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    /// <summary>A temp file that deletes itself on dispose.</summary>
    internal sealed class TempFile : IDisposable
    {
        private bool _disposed;

        internal TempFile(byte[] contents, string extension = ".dbf")
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "vfp-reader-" + Guid.NewGuid().ToString("N") + extension);
            File.WriteAllBytes(Path, contents);
        }

        /// <summary>The file path; it exists until the instance is disposed.</summary>
        internal string Path { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}

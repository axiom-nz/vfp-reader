using System.IO;

namespace VfpReader.Internal
{
    /// <summary>
    /// The library's only file-system dependency, behind one seam. Tests can substitute a fake
    /// through <see cref="Storage.Current"/> so memo discovery and path opening run without
    /// touching the disk; production always uses <see cref="DefaultStorage"/>.
    /// </summary>
    internal interface IStorage
    {
        /// <summary>Whether a file exists at <paramref name="path"/>.</summary>
        bool FileExists(string path);

        /// <summary>Opens a file read-only, allowing other readers to share it.</summary>
        Stream OpenRead(string path);
    }

    /// <summary>Holds the ambient <see cref="IStorage"/> used by the path-based entry points.</summary>
    internal static class Storage
    {
        private static IStorage _current = DefaultStorage.Instance;

        /// <summary>
        /// The file system used by <c>VfpTable.Open(string, …)</c>. Tests set this to a fake and
        /// restore it in a <c>finally</c>; production never changes it.
        /// </summary>
        internal static IStorage Current
        {
            get { return _current; }
            set { _current = value; }
        }
    }

    /// <summary>The real file system: <see cref="File"/> plus a shared-read <see cref="FileStream"/>.</summary>
    internal sealed class DefaultStorage : IStorage
    {
        internal static readonly DefaultStorage Instance = new DefaultStorage();

        private DefaultStorage()
        {
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public Stream OpenRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}

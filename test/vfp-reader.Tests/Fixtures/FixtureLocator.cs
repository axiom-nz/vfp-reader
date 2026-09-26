using System;
using System.IO;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Finds the real fixture corpus that the test project copies beside the binaries. Tests must
    /// never depend on the source-tree layout, so this only looks in the output directory.
    /// </summary>
    internal static class FixtureLocator
    {
        private static readonly string RootDirectory =
            Path.Combine(AppContext.BaseDirectory, "fixtures");

        /// <summary>Absolute path of a file under <c>test/fixtures/</c>.</summary>
        public static string PathOf(params string[] parts)
        {
            string path = RootDirectory;
            foreach (string part in parts)
            {
                path = Path.Combine(path, part);
            }

            return path;
        }

        /// <summary>A table or golden file from the canonical <c>ruby-dbf</c> corpus.</summary>
        public static string RubyDbf(string name)
        {
            return PathOf("ruby-dbf", name);
        }

        /// <summary>A file from the <c>dbfdatareader</c> drop.</summary>
        public static string DbfDataReader(string name)
        {
            return PathOf("dbfdatareader", name);
        }

        /// <summary>Whether the corpus was copied to the output directory.</summary>
        public static bool Copied
        {
            get { return Directory.Exists(RootDirectory); }
        }
    }
}

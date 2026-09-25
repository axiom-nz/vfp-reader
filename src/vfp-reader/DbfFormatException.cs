using System;
using System.Globalization;

namespace VfpReader
{
    /// <summary>
    /// Every structural problem found while reading a table. Carries the file path and, where it
    /// is known, the byte offset at which the problem was found.
    /// </summary>
    public sealed class DbfFormatException : Exception
    {
        /// <summary>Creates an exception with no context beyond a message.</summary>
        public DbfFormatException(string message)
            : base(message)
        {
        }

        /// <summary>Creates an exception wrapping another failure.</summary>
        public DbfFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Creates an exception naming the file and byte offset it was found at.</summary>
        public DbfFormatException(string message, string? path, long? offset)
            : base(Compose(message, path, offset))
        {
            Path = path;
            Offset = offset;
        }

        /// <summary>Creates an exception naming the file, byte offset and an inner failure.</summary>
        public DbfFormatException(string message, string? path, long? offset, Exception innerException)
            : base(Compose(message, path, offset), innerException)
        {
            Path = path;
            Offset = offset;
        }

        /// <summary>The table file being read, when it is known.</summary>
        public string? Path { get; }

        /// <summary>The byte offset the problem was found at, when it is known.</summary>
        public long? Offset { get; }

        private static string Compose(string message, string? path, long? offset)
        {
            if (path is null && offset is null)
            {
                return message;
            }

            if (path is null)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} (offset {1})", message, offset);
            }

            if (offset is null)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} (file '{1}')", message, path);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} (file '{1}', offset {2})", message, path, offset);
        }
    }
}

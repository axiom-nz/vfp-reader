using System;
using System.IO;

namespace VfpReader.Internal
{
    /// <summary>
    /// The one "read until the buffer is full" loop. `netstandard2.0` has no
    /// <c>Stream.ReadExactly</c> / <c>ReadAtLeast</c>, so callers pick their own error policy:
    /// <see cref="ReadAtMost"/> never throws and returns a short count, while
    /// <see cref="ReadFully"/> turns a short read into a <see cref="VfpFormatException"/>.
    /// </summary>
    internal static class StreamFill
    {
        /// <summary>
        /// Reads up to <paramref name="count"/> bytes, stopping early at the end of the stream or
        /// on an I/O failure. Never throws and never returns a negative count, so callers can treat
        /// a short read as missing data.
        /// </summary>
        internal static int ReadAtMost(Stream stream, byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n;
                try
                {
                    n = stream.Read(buffer, offset + read, count - read);
                }
                catch (IOException)
                {
                    return read;
                }

                if (n <= 0)
                {
                    break;
                }

                read += n;
            }

            return read;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes or throws. A failed or short read is
        /// reported as <paramref name="what"/> at <paramref name="path"/>.
        /// </summary>
        internal static void ReadFully(Stream stream, byte[] buffer, int offset, int count, string? path, string what)
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
                        "failed while reading " + what,
                        path,
                        offset + read,
                        ex);
                }

                if (n <= 0)
                {
                    throw new VfpFormatException(
                        "unexpected end of file while reading " + what,
                        path,
                        offset + read);
                }

                read += n;
            }
        }
    }
}

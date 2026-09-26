using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Reads the ruby-dbf <c>*_summary.txt</c> golden files. They are parsed structurally rather
    /// than compared line-for-line, so the exact column padding the Ruby suite emitted does not
    /// leak into our tests.
    /// </summary>
    internal static class SummaryFile
    {
        private static readonly Regex VersionPattern =
            new Regex(@"\(([0-9A-Fa-f]{2})\)", RegexOptions.CultureInvariant);

        public static ExpectedSummary Read(string path)
        {
            return Parse(File.ReadAllLines(path), path);
        }

        /// <summary>Parses summary text that the caller already read, for inline tests.</summary>
        public static ExpectedSummary Parse(string[] lines, string source)
        {
            string database = string.Empty;
            byte version = 0;
            bool hasMemo = false;
            long recordCount = -1;
            var fields = new List<ExpectedField>();
            bool inFields = false;

            foreach (string raw in lines)
            {
                string line = raw.TrimEnd();
                if (line.Length == 0)
                {
                    continue;
                }

                if (!inFields)
                {
                    if (line.StartsWith("Database:", StringComparison.Ordinal))
                    {
                        database = line.Substring("Database:".Length).Trim();
                    }
                    else if (line.StartsWith("Type:", StringComparison.Ordinal))
                    {
                        Match match = VersionPattern.Match(line);
                        if (!match.Success)
                        {
                            throw new InvalidDataException(
                                "no version byte in '" + line + "' of " + source);
                        }

                        version = byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("Memo File:", StringComparison.Ordinal))
                    {
                        string value = line.Substring("Memo File:".Length).Trim();
                        hasMemo = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (line.StartsWith("Records:", StringComparison.Ordinal))
                    {
                        string value = line.Substring("Records:".Length).Trim();
                        recordCount = long.Parse(value, CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("---", StringComparison.Ordinal))
                    {
                        inFields = true;
                    }

                    continue;
                }

                // The first line after the rule is the column header, which is not data.
                if (line.StartsWith("Name", StringComparison.Ordinal)
                    && line.IndexOf("Type", StringComparison.Ordinal) > 0)
                {
                    continue;
                }

                string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    throw new InvalidDataException("bad field row '" + line + "' in " + source);
                }

                if (parts[1].Length != 1)
                {
                    throw new InvalidDataException("bad field type '" + parts[1] + "' in " + source);
                }

                fields.Add(new ExpectedField(
                    parts[0],
                    parts[1][0],
                    int.Parse(parts[2], CultureInfo.InvariantCulture),
                    int.Parse(parts[3], CultureInfo.InvariantCulture)));
            }

            if (recordCount < 0 || fields.Count == 0)
            {
                throw new InvalidDataException("incomplete summary file " + source);
            }

            return new ExpectedSummary(database, version, hasMemo, recordCount, fields);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Reads one of the golden <c>.csv</c> dumps that ship beside the fixture tables. Cells are
    /// kept as raw strings: the value comparison is the reader's job, this type only preserves the
    /// reference bytes.
    /// </summary>
    internal sealed class GoldenCsv
    {
        private GoldenCsv(IReadOnlyList<string> header, IReadOnlyList<IReadOnlyList<string>> rows)
        {
            Header = header;
            Rows = rows;
        }

        public IReadOnlyList<string> Header { get; }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

        public static GoldenCsv Read(string path)
        {
            return Parse(File.ReadAllText(path, Encoding.UTF8), path);
        }

        /// <summary>Parses CSV text the caller already read, for inline tests.</summary>
        public static GoldenCsv Parse(string text, string source)
        {
            text = text.TrimStart('\uFEFF');
            var lines = new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            if (lines.Count == 0)
            {
                throw new InvalidDataException("empty CSV " + source);
            }

            var header = ParseLine(lines[0]);
            var rows = new List<IReadOnlyList<string>>();
            for (int i = 1; i < lines.Count; i++)
            {
                rows.Add(ParseLine(lines[i]));
            }

            return new GoldenCsv(header, rows);
        }

        /// <summary>Minimal RFC 4180 split: supports quoted cells and doubled quotes.</summary>
        private static IReadOnlyList<string> ParseLine(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else if (c == '"')
                {
                    quoted = true;
                }
                else if (c == ',')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(c);
                }
            }

            cells.Add(cell.ToString());
            return cells;
        }

        /// <summary>A canonical decimal form used to compare against reader values.</summary>
        public static string Decimal(string cell, int decimals)
        {
            return decimal.Parse(cell, NumberStyles.Float, CultureInfo.InvariantCulture)
                .ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}

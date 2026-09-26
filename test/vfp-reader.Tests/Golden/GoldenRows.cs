using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VfpReader.Tests.Fixtures;
using Xunit.Sdk;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// The seam between the golden rows and the reader's streaming API. Phase 2 implements the CSV
    /// comparison over fixed-width records; Phase 3 adds memo values and Phase 4 null flags and
    /// varlength, each removing the matching skips in <c>RowGoldenTests</c>.
    /// </summary>
    internal static class GoldenRows
    {
        /// <summary>
        /// Compares every CSV row against the reader, cell by cell, in file order. Each reader
        /// value is canonicalised to the text form the reference dump used, per field type.
        /// </summary>
        public static void CompareCsv(GoldenFixture fixture, VfpTable table)
        {
            GoldenCsv csv = GoldenCsv.Read(fixture.CsvPath!);
            IReadOnlyList<VfpField> fields = table.Schema.Fields;
            int rowIndex = 0;

            foreach (VfpRow row in table.ReadRows())
            {
                if (rowIndex >= csv.Rows.Count)
                {
                    throw new XunitException(
                        fixture.Name + ": the reader returned more rows than the golden " + csv.Rows.Count);
                }

                IReadOnlyList<string> expected = csv.Rows[rowIndex];
                for (int i = 0; i < fields.Count; i++)
                {
                    string actual = Canonical(fields[i], row[i]);
                    if (!string.Equals(expected[i], actual, StringComparison.Ordinal))
                    {
                        throw new XunitException(
                            fixture.Name + ": row " + rowIndex + " field " + fields[i].Name
                            + " expected <" + expected[i] + "> but was <" + actual + ">");
                    }
                }

                rowIndex++;
            }

            if (rowIndex != csv.Rows.Count)
            {
                throw new XunitException(
                    fixture.Name + ": expected " + csv.Rows.Count + " rows but read " + rowIndex);
            }
        }

        /// <summary>
        /// Compares one <c>.yml</c> golden record against the table's record of the same number,
        /// field by field. The YAML scalars are the reference implementation's Ruby text form
        /// (<c>87</c>, <c>0.0</c>, <c>true</c>, the memo text), so numeric and logical cells are
        /// compared by parsed value and text cells by string equality; only <c>M</c> reaches the
        /// memo file.
        /// </summary>
        public static void CompareRecord(GoldenFixture fixture, string recordPath, VfpTable table)
        {
            GoldenRecord record = GoldenRecord.Read(recordPath);
            IReadOnlyList<VfpField> fields = table.Schema.Fields;
            if (record.Values.Count != fields.Count)
            {
                throw new XunitException(
                    fixture.Name + ": the golden " + recordPath + " has " + record.Values.Count
                    + " values but the schema has " + fields.Count + " fields");
            }

            long recordNumber = RecordNumberOf(recordPath);
            VfpRow? row = null;
            foreach (VfpRow candidate in table.ReadRows())
            {
                if (candidate.RecordNumber == recordNumber)
                {
                    row = candidate;
                    break;
                }
            }

            if (row is null)
            {
                throw new XunitException(
                    fixture.Name + ": record " + recordNumber + " is missing from the reader's stream");
            }

            for (int i = 0; i < fields.Count; i++)
            {
                string expected = record.Values[i];
                object? actual = row[i];
                if (!Matches(fields[i], expected, actual))
                {
                    throw new XunitException(
                        fixture.Name + ": record " + recordNumber + " field " + fields[i].Name
                        + " expected <" + expected + "> but was <" + Describe(actual) + ">");
                }
            }
        }

        /// <summary>
        /// The 1-based record number a golden file pins. The file names use the reference
        /// implementation's zero-based record index, so <c>_record_0</c> is file record 1 and
        /// <c>_record_9</c> is file record 10.
        /// </summary>
        private static long RecordNumberOf(string recordPath)
        {
            string name = Path.GetFileNameWithoutExtension(recordPath);
            int marker = name.LastIndexOf("_record_", StringComparison.Ordinal);
            if (marker >= 0
                && long.TryParse(
                    name.Substring(marker + "_record_".Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long index))
            {
                return index + 1;
            }

            return 1;
        }

        private static bool Matches(VfpField field, string expected, object? actual)
        {
            switch (field.Type)
            {
                case VfpFieldType.Numeric:
                case VfpFieldType.Currency:
                    return actual is decimal numeric
                        && numeric == GoldenRecord.AsDecimal(expected);

                case VfpFieldType.Float:
                case VfpFieldType.Double:
                case VfpFieldType.DoubleO:
                    return actual is double real
                        && real == (double)GoldenRecord.AsDecimal(expected);

                case VfpFieldType.Logical:
                    return actual is bool flag && flag == GoldenRecord.AsBoolean(expected);

                case VfpFieldType.Date:
                    return actual is DateTime date
                        && date == DateTime.Parse(expected, CultureInfo.InvariantCulture);

                case VfpFieldType.DateTime:
                case VfpFieldType.DateTimeAt:
                    return actual is DateTime stamp
                        && stamp == DateTime.Parse(expected, CultureInfo.InvariantCulture);

                default:
                    if (expected.Length == 0)
                    {
                        return actual is null || (actual is string empty && empty.Length == 0);
                    }

                    return actual is string text && string.Equals(text, expected, StringComparison.Ordinal);
            }
        }

        private static string Describe(object? value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            if (value is byte[] bytes)
            {
                return Convert.ToBase64String(bytes);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string Canonical(VfpField field, object? value)
        {
            switch (field.Type)
            {
                case VfpFieldType.Character:
                    return value as string ?? string.Empty;

                case VfpFieldType.Logical:
                    return value is bool flag ? (flag ? "T" : "F") : string.Empty;

                case VfpFieldType.Integer:
                case VfpFieldType.AutoIncrement:
                    return value is int number ? number.ToString(CultureInfo.InvariantCulture) : string.Empty;

                case VfpFieldType.Numeric:
                    return value is decimal numeric
                        ? numeric.ToString("F" + field.Decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                        : string.Empty;

                case VfpFieldType.Currency:
                    // Y is an 8-byte integer of ten-thousandths: its scale is always 4, whatever
                    // the descriptor's decimals byte says.
                    return value is decimal amount
                        ? amount.ToString("F4", CultureInfo.InvariantCulture)
                        : string.Empty;

                case VfpFieldType.Float:
                    return value is double single
                        ? single.ToString("F" + field.Decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                        : string.Empty;

                case VfpFieldType.Double:
                case VfpFieldType.DoubleO:
                    return value is double real ? real.ToString(CultureInfo.InvariantCulture) : string.Empty;

                case VfpFieldType.Date:
                    return value is DateTime date
                        ? date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                        : string.Empty;

                case VfpFieldType.DateTime:
                case VfpFieldType.DateTimeAt:
                    return value is DateTime stamp
                        ? stamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                        : string.Empty;

                default:
                    return value is byte[] bytes ? Convert.ToBase64String(bytes) : string.Empty;
            }
        }
    }
}

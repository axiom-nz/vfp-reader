using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>Phase 3 owns memo values; the seam stays closed for now.</summary>
        public static void CompareRecord(GoldenFixture fixture, string recordPath, VfpTable table)
        {
            throw new NotSupportedException(
                "Phase 3: memo values are not available yet, so '" + recordPath + "' cannot be compared.");
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

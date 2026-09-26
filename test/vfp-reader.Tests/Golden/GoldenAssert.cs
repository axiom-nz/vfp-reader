using System;
using System.Collections.Generic;
using VfpReader.Tests.Fixtures;
using Xunit.Sdk;

namespace VfpReader.Tests.Golden
{
    /// <summary>
    /// Compares the reader's model against the golden files. Failures carry the offending field
    /// or row so a red golden test points straight at the gap instead of dumping whole tables.
    /// </summary>
    internal static class GoldenAssert
    {
        public static void Schema(string fixture, ExpectedSummary expected, DbfSchema actual)
        {
            AssertEqual(fixture + " version", expected.Version, actual.Version);
            AssertEqual(fixture + " record count", expected.RecordCount, actual.RecordCount);
            AssertEqual(fixture + " memo flag", expected.HasMemo, actual.HasMemo);

            IReadOnlyList<ExpectedField> expectedFields = expected.VisibleFields;
            if (expectedFields.Count != actual.FieldCount)
            {
                throw new XunitException(
                    fixture + ": field count expected " + expectedFields.Count + " but was " + actual.FieldCount);
            }

            for (int i = 0; i < expectedFields.Count; i++)
            {
                ExpectedField want = expectedFields[i];
                DbfField got = actual.Fields[i];
                string where = fixture + ": field[" + i + "]";

                AssertEqual(where + " name", want.Name, got.Name);
                AssertEqual(where + " type", want.Type, (char)got.Type);
                AssertEqual(where + " length", want.Length, (int)got.Length);
                AssertEqual(where + " decimals", want.Decimals, (int)got.Decimals);
            }
        }

        private static void AssertEqual<T>(string description, T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new XunitException(
                    description + " expected <" + Format(expected) + "> but was <" + Format(actual) + ">");
            }
        }

        private static string Format<T>(T value)
        {
            return value is null ? "null" : value.ToString() ?? string.Empty;
        }
    }
}

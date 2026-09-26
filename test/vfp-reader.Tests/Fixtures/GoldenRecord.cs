using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Reads the ruby-dbf <c>*.yml</c> record goldens. Each file is a single YAML sequence whose
    /// members are, positionally, the visible fields of one record. Scalars are kept as their
    /// YAML text form; <c>null</c> scalars become the empty string.
    /// </summary>
    internal sealed class GoldenRecord
    {
        private GoldenRecord(IReadOnlyList<string> values)
        {
            Values = values;
        }

        /// <summary>One value per visible field, in field order.</summary>
        public IReadOnlyList<string> Values { get; }

        public static GoldenRecord Read(string path)
        {
            return Parse(File.ReadAllText(path), path);
        }

        /// <summary>Parses YAML text the caller already read, for inline tests.</summary>
        public static GoldenRecord Parse(string yaml, string source)
        {
            var stream = new YamlStream();
            using (var reader = new StringReader(yaml))
            {
                stream.Load(reader);
            }

            if (stream.Documents.Count != 1
                || stream.Documents[0].RootNode is not YamlSequenceNode sequence)
            {
                throw new InvalidDataException("expected a single YAML sequence in " + source);
            }

            var values = new List<string>();
            foreach (YamlNode node in sequence.Children)
            {
                switch (node)
                {
                    case YamlScalarNode scalar:
                        values.Add(scalar.Value ?? string.Empty);
                        break;
                    default:
                        throw new InvalidDataException("expected scalar record values in " + source);
                }
            }

            return new GoldenRecord(values);
        }

        /// <summary>Parses a numeric golden scalar the way the reference dump wrote it.</summary>
        public static decimal AsDecimal(string value)
        {
            return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>Parses a logical golden scalar (<c>true</c>/<c>false</c>).</summary>
        public static bool AsBoolean(string value)
        {
            return bool.Parse(value);
        }
    }
}

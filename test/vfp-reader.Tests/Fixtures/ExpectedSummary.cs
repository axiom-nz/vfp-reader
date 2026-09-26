using System.Collections.Generic;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>One field row of a <c>*_summary.txt</c> golden file.</summary>
    internal sealed class ExpectedField
    {
        public ExpectedField(string name, char type, int length, int decimals)
        {
            Name = name;
            Type = type;
            Length = length;
            Decimals = decimals;
        }

        public string Name { get; }

        public char Type { get; }

        public int Length { get; }

        public int Decimals { get; }

        public override string ToString()
        {
            return Name + " " + Type + "(" + Length + "," + Decimals + ")";
        }
    }

    /// <summary>
    /// A parsed ruby-dbf <c>*_summary.txt</c>: what the reference implementation says about a
    /// table's version, record count and columns. Field rows are kept in file order, including
    /// the hidden <c>_NullFlags</c> field.
    /// </summary>
    internal sealed class ExpectedSummary
    {
        public ExpectedSummary(
            string database,
            byte version,
            bool hasMemo,
            long recordCount,
            IReadOnlyList<ExpectedField> fields)
        {
            Database = database;
            Version = version;
            HasMemo = hasMemo;
            RecordCount = recordCount;
            Fields = fields;
        }

        public string Database { get; }

        public byte Version { get; }

        public bool HasMemo { get; }

        public long RecordCount { get; }

        /// <summary>All field rows, in file order.</summary>
        public IReadOnlyList<ExpectedField> Fields { get; }

        /// <summary>Field rows the reader exposes, i.e. everything except <c>_NullFlags</c>.</summary>
        public IReadOnlyList<ExpectedField> VisibleFields
        {
            get
            {
                var visible = new List<ExpectedField>();
                foreach (ExpectedField field in Fields)
                {
                    if (field.Type != (char)VfpFieldType.NullFlags)
                    {
                        visible.Add(field);
                    }
                }

                return visible;
            }
        }
    }
}

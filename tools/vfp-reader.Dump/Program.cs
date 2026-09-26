using System;
using System.Text;
using VfpReader;

namespace VfpReader.Dump
{
    /// <summary>
    /// Prints the schema of a table, and later streams its rows as CSV. Used to compare this
    /// reader against real datasets and against Python's <c>dbfread</c>.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (args.Length < 1 || args[0] == "-h" || args[0] == "--help")
            {
                Console.Error.WriteLine("usage: vfp-reader.Dump <table.dbf> [table2.dbf ...]");
                return 2;
            }

            foreach (string path in args)
            {
                Dump(path);
            }

            return 0;
        }

        private static void Dump(string path)
        {
            using VfpTable table = VfpTable.Open(path);
            VfpSchema schema = table.Schema;

            Console.WriteLine("# " + path);
            Console.WriteLine(
                "version=0x{0:X2} records={1} header={2} record={3} codepage={4} memo={5} database={6}",
                schema.Version,
                schema.RecordCount,
                schema.HeaderLength,
                schema.RecordLength,
                schema.CodePage,
                schema.HasMemo,
                schema.DatabasePath.Length == 0 ? "-" : schema.DatabasePath);

            Console.WriteLine("# name\ttype\tlength\tdecimals\tnullable\tautoinc");
            foreach (VfpField field in schema.Fields)
            {
                Console.WriteLine(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                    field.Name,
                    (char)field.Type,
                    field.Length,
                    field.Decimals,
                    field.IsNullable,
                    field.IsAutoIncrement);
            }
        }
    }
}

using System.Linq;
using Xunit;

namespace VfpReader.Tests.Fixtures
{
    /// <summary>
    /// Row-fetch boilerplate shared by the record and memo tests. <c>Assert.Single(table.ReadRows())</c>
    /// appeared 36 times; these keep the same assertion (and failure output) with less noise.
    /// </summary>
    internal static class RowReader
    {
        /// <summary>Asserts the table yields exactly one row and returns it.</summary>
        internal static VfpRow Single(VfpTable table)
        {
            return Assert.Single(table.ReadRows());
        }

        /// <summary>Returns the first row, failing if the table is empty.</summary>
        internal static VfpRow First(VfpTable table)
        {
            return table.ReadRows().First();
        }
    }
}

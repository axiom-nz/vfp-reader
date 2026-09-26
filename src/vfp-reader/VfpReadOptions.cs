using System.Text;

namespace VfpReader
{
    /// <summary>How a table is opened and how its values are returned.</summary>
    public sealed class VfpReadOptions
    {
        /// <summary>
        /// The memo file to use. When <c>null</c> the sibling <c>.fpt</c> / <c>.dbt</c> file is
        /// found case-insensitively. Only consulted by
        /// <see cref="VfpTable.Open(string,VfpReadOptions?)"/>; a memo stream passed to
        /// <see cref="VfpTable.Open(System.IO.Stream,System.IO.Stream,VfpReadOptions)"/> wins.
        /// </summary>
        public string? MemoPath { get; set; }

        /// <summary>
        /// Overrides the code page recorded in header byte 29. Tables written with the wrong
        /// language driver are common, so this is the escape hatch. When <c>null</c>, leave it to
        /// the header's language driver (<see cref="VfpSchema.CodePage"/>), falling back to 1252.
        /// </summary>
        public Encoding? Encoding { get; set; }

        /// <summary>
        /// When true, deleted records are returned too; each carries <c>VfpRow.IsDeleted</c>. When
        /// false (the default) <c>VfpTable.ReadRows()</c> skips them.
        /// </summary>
        public bool IncludeDeleted { get; set; }

        /// <summary>
        /// When true (the default), trailing spaces and NULs are trimmed from <c>Character</c>
        /// values.
        /// </summary>
        public bool TrimCharacterFields { get; set; } = true;
    }
}

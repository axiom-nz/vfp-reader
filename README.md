# vfp-reader

A read-only, streaming .NET library that reads the schema and rows of Visual FoxPro, FoxPro 2.x
and dBASE III tables straight from disk. No OLE DB, no ODBC, no Visual FoxPro runtime, no 32-bit
process.

> **Status: Phase 1.** Header and field-descriptor parsing plus the schema are implemented.
> Streaming records, memo files, null bits and the `DbDataReader` adapter are still to come.
> See `.loop/PLAN.md` for the phase list.

## Scope

- Read a table's schema: field name, type, width, decimals, nullable, autoincrement.
- Stream rows one at a time with constant memory, whatever the table size.
- Return correct .NET values for every type, including real `null`s.
- Read memo text and binary from `.fpt` and `.dbt`.
- Handle any file extension (`.dbf`, Intravet's `.dat`) and caller-named memo files.

Not in scope: writing, indexes, querying, `.dbc` containers, record locking.

## Usage

```csharp
using VfpReader;

// On .NET Core, code pages other than UTF-8 need the provider registered once:
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

using var table = DbfTable.Open("invoice.dbf", new DbfReadOptions
{
    MemoPath = null,            // default: sibling .fpt / .dbt, case-insensitive
    Encoding = null,            // default: from header byte 29, 1252 when 0
    IncludeDeleted = false,
    TrimCharacterFields = true,
});

DbfSchema schema = table.Schema;
foreach (DbfField field in schema.Fields)
    Console.WriteLine($"{field.Name} {field.Type} {field.Length},{field.Decimals} null={field.IsNullable}");
```

## Layout

```
src/vfp-reader/         the library (netstandard2.0, System.Memory)
test/vfp-reader.Tests/  xUnit tests and the byte-level fixture builder
tools/vfp-reader.Dump/  console schema / CSV dump for real-data checks
```

## Attribution

The format rules follow FoxDevStudio's [`crates/foxvm/src/dbf`](https://github.com/FoxDevCommunity/FoxDevStudio)
(MIT). The code-page map and layout rules are a C# port of `encoding.rs` and `layout.rs`.

## License

MIT. See `LICENSE`.

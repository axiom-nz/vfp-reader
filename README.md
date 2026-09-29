# vfp-reader

A read-only .NET library that reads Visual FoxPro, FoxPro 2.x, FoxBASE+ and dBASE III / IV
tables (`.dbf`) directly from disk, together with their `.fpt` / `.dbt` memo files. It reads a
table's schema and streams its records one at a time. No OLE DB, no ODBC, no Visual FoxPro
runtime and no 32-bit process are required.

## Status

The library is pre-1.0. The API below is implemented and exercised against real tables, but it
may still change between 0.x releases.

Working today:

- Full header and field-descriptor parsing; the schema is available as soon as a table is opened.
- Streaming, forward-only record reading with constant memory, whatever the table size.
- Every fixed-width field type, `_NullFlags`-backed `null` values, and Visual FoxPro 9 `V` / `Q`
  varlength values.
- `.fpt` (FoxPro / FoxBASE+) and `.dbt` (dBASE III / IV) memo text and binary.

Not available yet:

- An ADO.NET `DbDataReader` / `GetSchemaTable` adapter.
- Asynchronous row streaming.
- `.dbc` database containers: a table's database backlink is reported, but long field names kept
  in the container are not resolved.

## Requirements

- The library targets `netstandard2.0`, so it runs on .NET Framework 4.7.2+ and .NET 10+.
- Non-UTF-8 tables on .NET Core / .NET 5+ need the code-page provider registered once at startup:

  ```csharp
  System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
  ```

Install the package from NuGet:

```shell
dotnet add package VfpReader
```

Alternatively, reference `src/vfp-reader` as a project and build from source.

## Supported tables

The version byte at header offset 0 selects the format and, when present, the memo layout:

| Version byte | Format | Memo file |
| --- | --- | --- |
| `0x30` | Visual FoxPro | optional `.fpt` |
| `0x31` | Visual FoxPro with autoincrement fields | optional `.fpt` |
| `0x32` | Visual FoxPro with `V` / `Q` varlength fields | optional `.fpt` |
| `0x83` | dBASE III with memo | `.dbt` |
| `0x8B` | dBASE IV with memo | `.dbt` |
| `0xF5` | FoxPro 2.x with memo | `.fpt` |
| `0xFB` | FoxBASE+ / dBASE IV with memo | `.fpt` |

FoxPro 2.x `0x02` (FoxBase) tables are not recognised.

## Quick start

```csharp
using System;
using System.Text;
using VfpReader;

// .NET Core / .NET 5+ only; required for any code page other than UTF-8.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

using var table = VfpTable.Open("customers.dbf");

Console.WriteLine(
    $"{table.Schema.FieldCount} fields, {table.Schema.RecordCount} records, " +
    $"code page {table.Schema.CodePage}");

foreach (VfpField field in table.Schema.Fields)
{
    Console.WriteLine(
        $"  {field.Name,-12} {(char)field.Type} {field.Length},{field.Decimals} " +
        $"nullable={field.IsNullable} bin={field.IsBinary}");
}

foreach (VfpRow row in table.ReadRows())
{
    object? id = row["CUSTNO"];  // case-insensitive name lookup
    object? name = row[1];       // or by ordinal
    Console.WriteLine($"{row.RecordNumber}\t{id}\t{name}");
}
```

## Reading options

`VfpReadOptions` controls how a table is opened and how values come back:

```csharp
using var table = VfpTable.Open("archive.dbf", new VfpReadOptions
{
    MemoPath = @"D:\memos\archive.fpt",      // default: the sibling .fpt / .dbt, case-insensitive
    Encoding = Encoding.GetEncoding(1251),   // default: header byte 29 (language driver)
    IncludeDeleted = true,                   // default: false
    TrimCharacterFields = false,             // default: true
});
```

| Property | Type | Default | Purpose |
| --- | --- | --- | --- |
| `MemoPath` | `string?` | `null` | The memo file to use. When `null`, the sibling `.fpt` / `.dbt` is found case-insensitively. Only consulted by the path-based `Open`; an explicit memo stream wins. |
| `Encoding` | `Encoding?` | `null` | Overrides the language driver recorded in header byte 29. The effective page is reported by `VfpSchema.CodePage`. |
| `IncludeDeleted` | `bool` | `false` | When `true`, soft-deleted records are returned too, each carrying `VfpRow.IsDeleted`. |
| `TrimCharacterFields` | `bool` | `true` | Trims trailing spaces and NULs from `C` and `V` text values. |

## Examples

### Stream a table to JSON

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using VfpReader;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

using var table = VfpTable.Open("customers.dbf");
using var json = File.Create("customers.json");
using var writer = new Utf8JsonWriter(json, new JsonWriterOptions { Indented = true });

writer.WriteStartArray();
foreach (VfpRow row in table.ReadRows())
{
    var record = new Dictionary<string, object?>();
    foreach (VfpField field in table.Schema.Fields)
        record[field.Name] = row[field.Name];

    JsonSerializer.Serialize(writer, record);
}
writer.WriteEndArray();

Console.WriteLine("wrote customers.json");
```

`Utf8JsonWriter` keeps the read streaming and constant-memory, and `System.Text.Json` picks the
right JSON shape from the runtime value: `decimal` / `double` / `int` become numbers, `bool` a
boolean, `DateTime` an ISO 8601 string, and `byte[]` a base64 string.

### Inspect deleted records and read memo columns

```csharp
using System;
using VfpReader;

// IncludeDeleted makes the skipped records visible so they can be counted.
using var table = VfpTable.Open("notes.dbf", new VfpReadOptions { IncludeDeleted = true });

long live = 0, deleted = 0;
foreach (VfpRow row in table.ReadRows())
{
    if (row.IsDeleted)
    {
        deleted++;
        continue;
    }

    live++;
    string? body = row["BODY"] as string;   // Memo (M) -> text from .fpt / .dbt
    byte[]? photo = row["PHOTO"] as byte[]; // General / Picture / Blob (G / P / W) -> bytes
    int preview = body is null ? 0 : Math.Min(body.Length, 40);
    Console.WriteLine($"#{row.RecordNumber} {body?.Substring(0, preview)} ({photo?.Length ?? 0} bytes)");
}

Console.WriteLine($"{live} live, {deleted} deleted");
```

### Open over caller-owned streams

```csharp
using System.IO;
using VfpReader;

using var dbf = File.OpenRead("data.dbf");
using var memo = File.OpenRead("data.fpt");
using var table = VfpTable.Open(dbf, memo); // the streams stay open; the caller owns them
```

## Value mapping

Every value is decoded to a .NET type. A `null` result means the field's `_NullFlags` bit is
set, or the stored value is blank, absent or unparseable.

| Field type | .NET value | Notes |
| --- | --- | --- |
| `C` (Character) | `string` | Trailing spaces / NULs trimmed when `TrimCharacterFields` is set. A binary `C` field returns `byte[]`. |
| `N` (Numeric) | `decimal?` | |
| `F` (Float) | `double?` | |
| `L` (Logical) | `bool?` | `T`/`Y` → `true`, `F`/`N` → `false`, anything else → `null`. |
| `D` (Date) | `DateTime?` | |
| `T` / `@` (DateTime) | `DateTime?` | |
| `I` (Integer) | `int` | |
| `+` (AutoIncrement) | `int` | |
| `Y` (Currency) | `decimal` | Stored value divided by 10,000. |
| `B` / `O` (Double) | `double` | |
| `M` (Memo) | `string?` | Text from the memo file, decoded with the table page. |
| `G` / `P` / `W` (General / Picture / Blob) | `byte[]?` | Raw memo-file bytes. |
| `V` (Varchar) | `string?` | Length comes from the varlength bit; trailing spaces / NULs trimmed as above. |
| `Q` (Varbinary) | `byte[]?` | Length comes from the varlength bit. |
| `Unknown` | `byte[]` | The raw field bytes. |

Nullability is driven by the hidden `_NullFlags` field. A field is only null-checked when it is
declared nullable; a non-nullable field never reads as `null` because of a stale bit. For a
`V` / `Q` field the flags field also carries a varlength bit: when it is set, the value's length
is the field's last byte (clamped so a malformed length cannot run past the record); otherwise
the value fills the whole field.

## Memo files

- The memo file is the sibling `.fpt` (FoxPro / FoxBASE+) or `.dbt` (dBASE III / IV) next to the
  table, matched case-insensitively. `MemoPath` overrides discovery; a memo stream passed to
  `Open(Stream, …)` wins over both.
- `M` reads a text block and decodes it with the table page. `G`, `P` and `W` read the non-text
  (picture / object) block and return raw bytes. A block whose kind does not match the field, a
  zero pointer, an out-of-range length or a missing memo file reads as `null` rather than
  throwing.
- A table flagged as needing a memo file opens fine when that file is absent; its memo columns
  simply read as `null`.

## Encoding and code pages

If `VfpReadOptions.Encoding` is set, it wins. Otherwise the language-driver byte at header
offset 29 is mapped to a `CodePage`; an unknown driver, including `0x00`, falls back to
`Windows1252`. On .NET Core / .NET 5+ the code-page provider must be registered before opening a
non-UTF-8 table, or opening throws `VfpFormatException`.

## Layout

```
src/vfp-reader/         the library (netstandard2.0, System.Memory)
test/vfp-reader.Tests/  xUnit tests and the byte-level fixture builder
tools/vfp-reader.Dump/  console schema dump for real-data checks
```

## Attribution

The format rules follow FoxDevStudio's
[`crates/foxvm/src/dbf`](https://github.com/FoxDevCommunity/FoxDevStudio) (MIT). The code-page
map and layout rules are a C# port of `encoding.rs` and `layout.rs`. The language-driver ID
table follows the dBase / FoxPro LDID list, as described by the XBase File Format Description
and reproduced by `dbfread` and Ethan Furman's `dbf`.

## License

MIT. See `LICENSE`.

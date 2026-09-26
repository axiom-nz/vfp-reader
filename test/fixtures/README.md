# Fixtures

Real tables copied from MIT-licensed projects, plus the expected-output files that came with
them. Format rules that do not need a real table are pinned in code instead, by the byte-level
`DbfBuilder` in `../vfp-reader.Tests/Fixtures/DbfBuilder.cs`.

See `NOTICE` for the licenses and the exact upstream sources.

## Layout

```
ruby-dbf/       the canonical corpus, copied verbatim from infused/dbf `spec/fixtures/`
dbfdatareader/  one extra table copied from yellowfeather/DbfDataReader, renamed
```

### `ruby-dbf/` — canonical corpus

[`infused/dbf`](https://github.com/infused/dbf) (the Ruby `dbf` gem) is the origin of the
`dbase_*` fixtures that both .NET projects reuse. It is copied verbatim and is the reference
copy: the forks have drifted (for example DbfDataReader's `dbase_03`, `dbase_30` and `dbase_31`
differ byte-for-byte from this one), so always compare against this directory.

| Fixture | Version | Fields | What it exercises |
|---|---|---|---|
| `dbase_02.dbf` | `0x02` FoxBase | 14 | `N`, `C` |
| `dbase_03.dbf` | `0x03` dBASE III | 31 | `C`, `D`, `N`, deleted rows |
| `dbase_03_cyrillic.dbf` | `0x03` | 2 | code page / Cyrillic bytes |
| `dbase_30.dbf` + `.fpt` | `0x30` Visual FoxPro | 145 | `B`, `C`, `D`, `F`, `L`, `M`, `N`, `T` |
| `dbase_31.dbf` | `0x31` VFP + autoincrement | 11 | `I`, `Y`, `L`, `C` |
| `dbase_32.dbf` | `0x32` VFP varchar | 2 | **`V` varlength + `_NullFlags` (`0`)** |
| `dbase_83.dbf` + `.dbt` | `0x83` dBASE III + memo | 15 | `C`, `L`, `M`, `N` |
| `dbase_8b.dbf` + `.dbt` | `0x8b` dBASE IV + memo | 6 | `D`, `F`, `L`, `M`, `N` |
| `dbase_8c.dbf` | `0x8c` dBASE IV + memo | — | version variant |
| `dbase_f5.dbf` + `.fpt` | `0xf5` FoxPro + memo | 59 | `C`, `D`, `M`, `N` |
| `cp1251.dbf` | `0x30` | 1 | Windows-1251 code page |
| `foxprodb/` | `0x30` | — | database container and sample tables |
| `mazovia.dbf`, `polygon.dbf` | — | — | extra encoding/geometry samples |

Alongside the tables are the golden files the Ruby suite generated:

- `*_summary.txt` — schema dump: `Name`, `Type`, `Length`, `Decimal` per field.
- `dbase_83_record_0.yml`, `dbase_83_record_9.yml`, `dbase_83_missing_memo_record_0.yml` —
  full expected records.
- `dbase_83_schema_*.txt` — schema projections.
- `foxprodb/*.CDX`, `*.DBC`/`.DCX`/`.DCT` — indexes and container, out of scope for v1 but kept
  so the corpus stays intact.

### `dbfdatareader/` — `dbase_31_nullflags`

[`yellowfeather/DbfDataReader`](https://github.com/yellowfeather/DbfDataReader) modified the
canonical `dbase_31` by adding `FLOAT F`, `DOUBLE B` and a `_NullFlags 0` field (13 fields
instead of 11). That variant is useful for Phase 2/4, so it is copied here under a distinct
name; it does not overwrite `ruby-dbf/dbase_31.dbf`.

- `dbase_31_nullflags.dbf` — the table.
- `dbase_31_nullflags_summary.txt` — upstream `dbase_31_summary.txt`.
- `dbase_31_nullflags.csv` — upstream golden rows, every cell, including the hidden `_NullFlags` column. (Unlike DbfDataReader's `dbase_03.csv`, this dump has no trailing `deleted` column.)

## Using the golden files

`*_summary.txt` is read after its `---` separator, one field per line, at the fixed columns the
Ruby suite used. The `.csv` files are complete dumps: compare them cell-for-cell against
`vfp-reader.Dump` in the Phase 6 real-data diff. The `.yml` files pin whole records.

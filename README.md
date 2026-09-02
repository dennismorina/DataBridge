# DataBridge

[![CI](https://github.com/dennismorina/DataBridge/actions/workflows/ci.yml/badge.svg)](https://github.com/dennismorina/DataBridge/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED)
![License](https://img.shields.io/badge/License-MIT-green)

A production-oriented data import pipeline for CSV and Excel files.

DataBridge focuses on a common backend problem: receiving external business data,
validating and normalizing it, importing valid records efficiently, and producing a
clear reject report for invalid rows.

Unlike a typical CRUD demo, this project concentrates on file processing, SQL,
idempotency, batch imports and operational reliability.

## Highlights

- .NET 10 / C#
- Command-line import tool
- CSV and XLSX input
- Streaming source readers
- Validation and normalization
- Duplicate detection inside source files
- Configurable batch size
- PostgreSQL 17
- Native Npgsql binary `COPY`
- Temporary staging tables
- Set-based SQL `UPSERT`
- Automatic SQL migrations
- Import history
- SHA-256 based idempotency
- Reject reports
- Dry-run mode
- Docker / Docker Compose
- Unit and integration tests
- Code coverage
- GitHub Actions CI
- Real PostgreSQL smoke test in CI
- Dependabot

## Architecture

```text
                  CSV / XLSX
                     |
                     v
        +-------------------------+
        |      Source Reader      |
        | CsvHelper / ClosedXML   |
        +------------+------------+
                     |
                     v
        +-------------------------+
        | Validation / Normalize  |
        | SKU / Name / Price / Qty|
        +-------+-----------+-----+
                |           |
            valid           invalid
                |           |
                v           v
       +--------------+  +---------------+
       | Batch Buffer |  | Reject Report |
       +------+-------+  +---------------+
              |
              v
       PostgreSQL Binary COPY
              |
              v
       Temporary Stage Table
              |
              v
        INSERT ... ON CONFLICT
              |
              v
          products table
```

## Project Structure

```text
DataBridge
├── src
│   ├── DataBridge.Cli
│   ├── DataBridge.Application
│   ├── DataBridge.Domain
│   └── DataBridge.Infrastructure
├── tests
│   ├── DataBridge.UnitTests
│   └── DataBridge.IntegrationTests
├── samples
│   └── products.csv
├── output
├── .github
│   ├── workflows
│   │   └── ci.yml
│   └── dependabot.yml
├── docker-compose.yml
├── DataBridge.sln
└── README.md
```

## Input Format

Supported formats:

```text
.csv
.xlsx
```

Required columns:

| Column | Description |
|---|---|
| `sku` | Unique product SKU |
| `name` | Product name |
| `price` | Non-negative decimal |
| `stockQuantity` | Non-negative integer |

Header matching ignores case, spaces, `_` and `-`, so both `stockQuantity` and
`stock_quantity` are accepted.

SKUs are trimmed and normalized to uppercase before persistence.

## Dry Run

A dry run validates the full source without PostgreSQL:

```bash
dotnet run --project src/DataBridge.Cli -- \
  import \
  --file samples/products.csv \
  --reject-file output/rejected.csv \
  --dry-run
```

The included sample contains five valid and three intentionally invalid rows.

## PostgreSQL

Start the database:

```bash
docker compose up -d postgres
```

The host port is intentionally `5434`:

```text
localhost:5434 -> container:5432
```

Local connection string:

```text
Host=localhost;Port=5434;Database=databridge;Username=app;Password=app_password
```

PowerShell:

```powershell
$env:DATABRIDGE_CONNECTION_STRING = "Host=localhost;Port=5434;Database=databridge;Username=app;Password=app_password"
```

## Run an Import

```bash
dotnet run --project src/DataBridge.Cli -- \
  import \
  --file samples/products.csv \
  --reject-file output/rejected.csv
```

The schema is migrated automatically before importing.

Expected sample result:

```text
Rows:       8
Accepted:   5
Rejected:   3
Upserted:   5
```

## Import Strategy

Valid records are processed in bounded batches. Each batch:

1. creates a PostgreSQL temporary staging table
2. loads records with binary `COPY`
3. performs one set-based `INSERT ... ON CONFLICT`
4. commits the transaction
5. continues with the next batch

This avoids one database round trip per source row.

Default batch size:

```text
1000
```

Custom batch size:

```bash
dotnet run --project src/DataBridge.Cli -- \
  import \
  --file samples/products.csv \
  --batch-size 5000
```

Allowed range: `1` to `50,000`.

## Idempotency

DataBridge computes a SHA-256 hash for each source file.

A successful hash is stored in `import_jobs`. Submitting the exact same file again is
skipped automatically.

To intentionally run the same file again:

```bash
dotnet run --project src/DataBridge.Cli -- \
  import \
  --file samples/products.csv \
  --force
```

Persistence itself is also idempotent by SKU because PostgreSQL uses an UPSERT.

## Reject Reports

Invalid source records are not silently ignored. They are written to a CSV report with:

- source row number
- original values
- validation reason

Examples include missing SKUs, invalid prices, negative values and duplicate SKUs
inside the same source file.

## Database Schema

DataBridge creates:

```text
schema_migrations
products
import_jobs
```

`schema_migrations` tracks embedded SQL migrations.

`products` stores the current normalized product state.

`import_jobs` stores operational history including hash, status, counts, duration and
errors.

No ORM is used for the import path. The project intentionally demonstrates direct
PostgreSQL access, bulk transfer and set-based SQL.

## Docker

Run the import inside Docker:

```bash
docker compose run --rm databridge \
  import \
  --file /app/samples/products.csv \
  --reject-file /app/output/rejected.csv
```

Inside Docker the CLI connects to:

```text
Host=postgres
Port=5432
```

The host uses port `5434`.

Stop everything:

```bash
docker compose down
```

Remove PostgreSQL data as well:

```bash
docker compose down -v
```

## Testing

```bash
dotnet test --solution DataBridge.sln --configuration Release
```

The solution contains tests for:

- domain validation
- number parsing
- duplicate detection
- CSV reading
- Excel reading
- reject report generation

CI additionally executes a real containerized import against PostgreSQL and checks the
database row count.

## Continuous Integration

Every push and pull request targeting `main` runs:

```text
Restore
   |
Release Build
   |
Unit + Integration Tests
   |
Code Coverage
   |
CLI Dry-Run Smoke Test
   |
Docker Build
   |
PostgreSQL Container
   |
Real Import
   |
Database Verification
```

## CLI Reference

```text
DataBridge import --file <path> [options]

--file <path>          CSV or XLSX source file
--reject-file <path>   reject CSV path
--connection <value>   PostgreSQL connection string
--batch-size <n>       1 - 50,000; default 1000
--dry-run              validate without database writes
--force                bypass successful-file idempotency
--help                 show help
```

## Technology Stack

| Area | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| CSV | CsvHelper |
| Excel | ClosedXML |
| Database | PostgreSQL 17 |
| Driver | Npgsql |
| Bulk Import | PostgreSQL Binary COPY |
| Persistence | Staging + UPSERT |
| Testing | xUnit |
| Coverage | Coverlet |
| Containers | Docker / Docker Compose |
| CI | GitHub Actions |
| Dependency Updates | Dependabot |

## Design Goals

DataBridge demonstrates backend work frequently found in real business systems:

- external data ingestion
- validation of imperfect input
- deterministic normalization
- accepted/rejected separation
- safe repeat processing
- bounded batch processing
- set-based SQL
- migration history
- operational import history
- reproducible infrastructure
- end-to-end CI verification

The project deliberately remains focused on data integration instead of becoming
another general-purpose web API.

## License

MIT.

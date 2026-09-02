using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;
using DataBridge.Domain;
using Npgsql;
using NpgsqlTypes;

namespace DataBridge.Infrastructure.Database;

public sealed class PostgresDataStore : IImportDataStore
{
    private readonly string _connectionString;

    public PostgresDataStore(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "A PostgreSQL connection string is required.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        new MigrationRunner(_connectionString).RunAsync(cancellationToken);

    public async Task<bool> HasSuccessfulImportAsync(
        string sourceHash,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM import_jobs
                WHERE source_hash = @source_hash
                  AND status = 'Succeeded'
            );
            """,
            connection);

        command.Parameters.AddWithValue("source_hash", sourceHash);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<int> UpsertBatchAsync(
        IReadOnlyCollection<ProductRecord> products,
        CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return 0;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var createStaging = new NpgsqlCommand(
            """
            CREATE TEMP TABLE product_import_stage
            (
                sku varchar(50) NOT NULL,
                name varchar(200) NOT NULL,
                price numeric(18,2) NOT NULL,
                stock_quantity integer NOT NULL
            )
            ON COMMIT DROP;
            """,
            connection,
            transaction))
        {
            await createStaging.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var importer = await connection.BeginBinaryImportAsync(
            """
            COPY product_import_stage (sku, name, price, stock_quantity)
            FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken))
        {
            foreach (var product in products)
            {
                await importer.StartRowAsync(cancellationToken);
                await importer.WriteAsync(product.Sku, NpgsqlDbType.Varchar, cancellationToken);
                await importer.WriteAsync(product.Name, NpgsqlDbType.Varchar, cancellationToken);
                await importer.WriteAsync(product.Price, NpgsqlDbType.Numeric, cancellationToken);
                await importer.WriteAsync(product.StockQuantity, NpgsqlDbType.Integer, cancellationToken);
            }

            await importer.CompleteAsync(cancellationToken);
        }

        int affected;

        await using (var upsert = new NpgsqlCommand(
            """
            INSERT INTO products
            (
                sku,
                name,
                price,
                stock_quantity,
                created_at,
                updated_at
            )
            SELECT
                sku,
                name,
                price,
                stock_quantity,
                now(),
                now()
            FROM product_import_stage
            ON CONFLICT (sku)
            DO UPDATE SET
                name = EXCLUDED.name,
                price = EXCLUDED.price,
                stock_quantity = EXCLUDED.stock_quantity,
                updated_at = now();
            """,
            connection,
            transaction))
        {
            affected = await upsert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return affected;
    }

    public async Task RecordImportAsync(
        ImportJob job,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO import_jobs
            (
                id,
                file_name,
                source_hash,
                status,
                total_rows,
                accepted_rows,
                rejected_rows,
                upserted_rows,
                duration_ms,
                started_at,
                completed_at,
                error_message
            )
            VALUES
            (
                @id,
                @file_name,
                @source_hash,
                @status,
                @total_rows,
                @accepted_rows,
                @rejected_rows,
                @upserted_rows,
                @duration_ms,
                @started_at,
                @completed_at,
                @error_message
            );
            """,
            connection);

        command.Parameters.AddWithValue("id", job.Id);
        command.Parameters.AddWithValue("file_name", job.FileName);
        command.Parameters.AddWithValue("source_hash", job.SourceHash);
        command.Parameters.AddWithValue("status", job.Status);
        command.Parameters.AddWithValue("total_rows", job.TotalRows);
        command.Parameters.AddWithValue("accepted_rows", job.AcceptedRows);
        command.Parameters.AddWithValue("rejected_rows", job.RejectedRows);
        command.Parameters.AddWithValue("upserted_rows", job.UpsertedRows);
        command.Parameters.AddWithValue("duration_ms", job.DurationMilliseconds);
        command.Parameters.AddWithValue("started_at", job.StartedAt);
        command.Parameters.AddWithValue("completed_at", job.CompletedAt);
        command.Parameters.AddWithValue(
            "error_message",
            (object?)job.ErrorMessage ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

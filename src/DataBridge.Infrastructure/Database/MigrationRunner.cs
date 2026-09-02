using System.Reflection;
using Npgsql;

namespace DataBridge.Infrastructure.Database;

internal sealed class MigrationRunner
{
    private readonly string _connectionString;

    public MigrationRunner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureMigrationTableAsync(connection, cancellationToken);

        var assembly = typeof(MigrationRunner).Assembly;
        var migrations = assembly.GetManifestResourceNames()
            .Where(name =>
                name.Contains(".Migrations.", StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var migration in migrations)
        {
            if (await IsAppliedAsync(connection, migration, cancellationToken))
            {
                continue;
            }

            var sql = await ReadResourceAsync(assembly, migration, cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using (var migrationCommand = new NpgsqlCommand(sql, connection, transaction))
            {
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var historyCommand = new NpgsqlCommand(
                """
                INSERT INTO schema_migrations (version)
                VALUES (@version);
                """,
                connection,
                transaction))
            {
                historyCommand.Parameters.AddWithValue("version", migration);
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task EnsureMigrationTableAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS schema_migrations
            (
                version text PRIMARY KEY,
                applied_at timestamptz NOT NULL DEFAULT now()
            );
            """,
            connection);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsAppliedAsync(
        NpgsqlConnection connection,
        string migration,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM schema_migrations
                WHERE version = @version
            );
            """,
            connection);

        command.Parameters.AddWithValue("version", migration);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task<string> ReadResourceAsync(
        Assembly assembly,
        string resourceName,
        CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded migration '{resourceName}' could not be loaded.");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}

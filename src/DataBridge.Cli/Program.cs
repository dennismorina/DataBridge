using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;
using DataBridge.Application.Services;
using DataBridge.Infrastructure.Database;
using DataBridge.Infrastructure.Files;

namespace DataBridge.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (!CliOptionsParser.TryParse(args, out var cliOptions, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine($"Error: {error}");
                Console.Error.WriteLine();
            }

            CliOptionsParser.PrintHelp();
            return string.IsNullOrWhiteSpace(error) ? 0 : 2;
        }

        if (cliOptions is null)
        {
            return 2;
        }

        try
        {
            IImportDataStore dataStore = cliOptions.DryRun
                ? new DryRunImportDataStore()
                : new PostgresDataStore(cliOptions.ConnectionString!);

            await dataStore.InitializeAsync();

            var service = new ImportService(
                new ProductSourceReaderFactory(),
                new ProductRowValidator(),
                new CsvRejectWriter(),
                dataStore);

            var result = await service.ImportAsync(
                new ImportOptions(
                    cliOptions.FilePath,
                    cliOptions.RejectFilePath,
                    cliOptions.DryRun,
                    cliOptions.Force,
                    cliOptions.BatchSize));

            PrintResult(result);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Import failed.");
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void PrintResult(ImportResult result)
    {
        Console.WriteLine();
        Console.WriteLine("DataBridge import result");
        Console.WriteLine("------------------------");
        Console.WriteLine($"File:       {result.FileName}");
        Console.WriteLine($"SHA-256:    {result.SourceHash}");

        if (result.SkippedAlreadyProcessed)
        {
            Console.WriteLine("Status:     skipped - identical source was already imported successfully");
            return;
        }

        Console.WriteLine($"Mode:       {(result.DryRun ? "dry-run" : "database import")}");
        Console.WriteLine($"Rows:       {result.TotalRows}");
        Console.WriteLine($"Accepted:   {result.AcceptedRows}");
        Console.WriteLine($"Rejected:   {result.RejectedRows}");
        Console.WriteLine($"Upserted:   {result.UpsertedRows}");
        Console.WriteLine($"Duration:   {result.Duration.TotalMilliseconds:N0} ms");

        if (!string.IsNullOrWhiteSpace(result.RejectFilePath))
        {
            Console.WriteLine($"Rejects:    {result.RejectFilePath}");
        }
    }
}

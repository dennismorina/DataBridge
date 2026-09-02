using System.Diagnostics;
using System.Security.Cryptography;
using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;
using DataBridge.Domain;

namespace DataBridge.Application.Services;

public sealed class ImportService
{
    private readonly IProductSourceReaderFactory _readerFactory;
    private readonly ProductRowValidator _validator;
    private readonly IRejectWriter _rejectWriter;
    private readonly IImportDataStore _dataStore;

    public ImportService(
        IProductSourceReaderFactory readerFactory,
        ProductRowValidator validator,
        IRejectWriter rejectWriter,
        IImportDataStore dataStore)
    {
        _readerFactory = readerFactory;
        _validator = validator;
        _rejectWriter = rejectWriter;
        _dataStore = dataStore;
    }

    public async Task<ImportResult> ImportAsync(
        ImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.BatchSize is < 1 or > 50_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Batch size must be between 1 and 50,000.");
        }

        if (!File.Exists(options.FilePath))
        {
            throw new FileNotFoundException("Import file was not found.", options.FilePath);
        }

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var sourceHash = await ComputeSha256Async(options.FilePath, cancellationToken);
        var fileName = Path.GetFileName(options.FilePath);

        if (!options.DryRun
            && !options.Force
            && await _dataStore.HasSuccessfulImportAsync(sourceHash, cancellationToken))
        {
            stopwatch.Stop();

            return new ImportResult(
                fileName,
                sourceHash,
                0,
                0,
                0,
                0,
                null,
                false,
                true,
                stopwatch.Elapsed);
        }

        var reader = _readerFactory.Create(options.FilePath);
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rejected = new List<RejectedRow>();
        var batch = new List<ProductRecord>(options.BatchSize);

        var totalRows = 0;
        var acceptedRows = 0;
        var upsertedRows = 0;

        try
        {
            await foreach (var row in reader.ReadAsync(options.FilePath, cancellationToken))
            {
                totalRows++;

                var validation = _validator.Validate(row);

                if (!validation.IsValid || validation.Product is null)
                {
                    rejected.Add(CreateRejectedRow(row, validation.Errors));
                    continue;
                }

                if (!seenSkus.Add(validation.Product.Sku))
                {
                    rejected.Add(
                        CreateRejectedRow(
                            row,
                            ["Duplicate SKU in source file."]));
                    continue;
                }

                acceptedRows++;
                batch.Add(validation.Product);

                if (batch.Count >= options.BatchSize)
                {
                    upsertedRows += await FlushBatchAsync(
                        batch,
                        options.DryRun,
                        cancellationToken);
                }
            }

            if (batch.Count > 0)
            {
                upsertedRows += await FlushBatchAsync(
                    batch,
                    options.DryRun,
                    cancellationToken);
            }

            string? rejectFilePath = null;

            if (rejected.Count > 0)
            {
                await _rejectWriter.WriteAsync(
                    options.RejectFilePath,
                    rejected,
                    cancellationToken);

                rejectFilePath = options.RejectFilePath;
            }

            stopwatch.Stop();

            if (!options.DryRun)
            {
                await _dataStore.RecordImportAsync(
                    new ImportJob(
                        Guid.NewGuid(),
                        fileName,
                        sourceHash,
                        "Succeeded",
                        totalRows,
                        acceptedRows,
                        rejected.Count,
                        upsertedRows,
                        (long)stopwatch.Elapsed.TotalMilliseconds,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        null),
                    cancellationToken);
            }

            return new ImportResult(
                fileName,
                sourceHash,
                totalRows,
                acceptedRows,
                rejected.Count,
                upsertedRows,
                rejectFilePath,
                options.DryRun,
                false,
                stopwatch.Elapsed);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            if (!options.DryRun)
            {
                try
                {
                    await _dataStore.RecordImportAsync(
                        new ImportJob(
                            Guid.NewGuid(),
                            fileName,
                            sourceHash,
                            "Failed",
                            totalRows,
                            acceptedRows,
                            rejected.Count,
                            upsertedRows,
                            (long)stopwatch.Elapsed.TotalMilliseconds,
                            startedAt,
                            DateTimeOffset.UtcNow,
                            exception.Message),
                        cancellationToken);
                }
                catch
                {
                    // Preserve the original import exception.
                }
            }

            throw;
        }
    }

    private async Task<int> FlushBatchAsync(
        List<ProductRecord> batch,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var count = dryRun
            ? batch.Count
            : await _dataStore.UpsertBatchAsync(batch, cancellationToken);

        batch.Clear();
        return count;
    }

    private static RejectedRow CreateRejectedRow(
        RawProductRow row,
        IReadOnlyCollection<string> errors) =>
        new(
            row.RowNumber,
            row.Sku,
            row.Name,
            row.Price,
            row.StockQuantity,
            string.Join(" ", errors));

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

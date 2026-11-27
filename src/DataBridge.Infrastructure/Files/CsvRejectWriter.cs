using System.Globalization;
using CsvHelper;
using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;

namespace DataBridge.Infrastructure.Files;

public sealed class CsvRejectWriter : IRejectWriter
{
    public Task WriteAsync(
        string filePath,
        IReadOnlyCollection<RejectedRow> rows,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);

        using var csv = CsvWriter.ToFile(filePath, new CsvWriterOptions
        {
            Quoting = CsvQuoting.WhenNeededOrWhitespace
        });

        csv.WriteHeader("RowNumber", "Sku", "Name", "Price", "StockQuantity", "Reason");

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            csv.WriteRecord(row.RowNumber.ToString(CultureInfo.InvariantCulture),
                row.Sku, row.Name, row.Price, row.StockQuantity, row.Reason);
        }

        return Task.CompletedTask;
    }
}

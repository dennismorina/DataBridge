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

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteHeader<RejectedRow>();
        csv.NextRecord();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            csv.WriteRecord(row);
            csv.NextRecord();
        }

        writer.Flush();
        return Task.CompletedTask;
    }
}

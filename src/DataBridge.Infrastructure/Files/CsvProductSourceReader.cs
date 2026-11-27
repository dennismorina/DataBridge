using System.Runtime.CompilerServices;
using CsvHelper;
using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;

namespace DataBridge.Infrastructure.Files;

public sealed class CsvProductSourceReader : IProductSourceReader
{
    public async IAsyncEnumerable<RawProductRow> ReadAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(filePath);

        var options = new CsvReaderOptions
        {
            Separator = ',',
            NormalizeHeaderName = HeaderNormalizer.Normalize,
            TrimWhitespace = true,
            MalformedData = CsvMalformedDataHandling.Throw,
            MissingField = CsvMissingFieldHandling.Throw
        };

        using var csv = CsvReader.Create(streamReader, options);

        await csv.ReadHeaderAsync(cancellationToken);

        if (csv.Headers.Count == 0)
        {
            yield break;
        }

        ValidateHeaders(csv.Headers);

        var rowNumber = 1;

        while (await csv.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            yield return new RawProductRow(
                rowNumber,
                csv.GetString("sku"),
                csv.GetString("name"),
                csv.GetString("price"),
                csv.GetString("stockquantity"));
        }
    }

    private static void ValidateHeaders(IReadOnlyList<string> headers)
    {
        var normalizedHeaders = headers
            .Select(HeaderNormalizer.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var required = new[] { "sku", "name", "price", "stockquantity" };
        var missing = required
            .Where(x => !normalizedHeaders.Contains(x))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Missing required column(s): {string.Join(", ", missing)}.");
        }
    }
}

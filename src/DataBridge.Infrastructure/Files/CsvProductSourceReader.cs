using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper;
using CsvHelper.Configuration;
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

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => HeaderNormalizer.Normalize(args.Header),
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(streamReader, configuration);

        if (!await csv.ReadAsync())
        {
            yield break;
        }

        csv.ReadHeader();
        ValidateHeaders(csv.HeaderRecord);

        var rowNumber = 1;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowNumber++;

            yield return new RawProductRow(
                rowNumber,
                csv.GetField("sku") ?? string.Empty,
                csv.GetField("name") ?? string.Empty,
                csv.GetField("price") ?? string.Empty,
                csv.GetField("stockquantity") ?? string.Empty);
        }
    }

    private static void ValidateHeaders(string[]? headers)
    {
        var normalizedHeaders = (headers ?? [])
            .Select(HeaderNormalizer.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var required = new[] { "sku", "name", "price", "stockquantity" };
        var missing = required.Where(x => !normalizedHeaders.Contains(x)).ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Missing required column(s): {string.Join(", ", missing)}.");
        }
    }
}

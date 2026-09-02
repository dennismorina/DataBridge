using System.Runtime.CompilerServices;
using ClosedXML.Excel;
using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;

namespace DataBridge.Infrastructure.Files;

public sealed class ExcelProductSourceReader : IProductSourceReader
{
    public async IAsyncEnumerable<RawProductRow> ReadAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        var firstRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException("The Excel file does not contain a header row.");

        var headerMap = firstRow.CellsUsed()
            .ToDictionary(
                cell => HeaderNormalizer.Normalize(cell.GetString()),
                cell => cell.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

        var skuColumn = GetRequiredColumn(headerMap, "sku");
        var nameColumn = GetRequiredColumn(headerMap, "name");
        var priceColumn = GetRequiredColumn(headerMap, "price");
        var stockColumn = GetRequiredColumn(headerMap, "stockquantity");

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();

        for (var rowNumber = firstRow.RowNumber() + 1;
             rowNumber <= lastRowNumber;
             rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);

            yield return new RawProductRow(
                rowNumber,
                row.Cell(skuColumn).GetString(),
                row.Cell(nameColumn).GetString(),
                row.Cell(priceColumn).GetString(),
                row.Cell(stockColumn).GetString());

            await Task.Yield();
        }
    }

    private static int GetRequiredColumn(
        IReadOnlyDictionary<string, int> headerMap,
        string column)
    {
        if (!headerMap.TryGetValue(column, out var columnNumber))
        {
            throw new InvalidDataException($"Missing required column: {column}.");
        }

        return columnNumber;
    }
}

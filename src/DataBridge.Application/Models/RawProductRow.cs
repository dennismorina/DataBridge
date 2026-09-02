namespace DataBridge.Application.Models;

public sealed record RawProductRow(
    int RowNumber,
    string Sku,
    string Name,
    string Price,
    string StockQuantity);

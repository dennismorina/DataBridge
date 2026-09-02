namespace DataBridge.Application.Models;

public sealed record RejectedRow(
    int RowNumber,
    string Sku,
    string Name,
    string Price,
    string StockQuantity,
    string Reason);

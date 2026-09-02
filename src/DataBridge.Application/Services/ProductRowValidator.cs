using System.Globalization;
using DataBridge.Application.Models;
using DataBridge.Domain;

namespace DataBridge.Application.Services;

public sealed class ProductRowValidator
{
    public RowValidationResult Validate(RawProductRow row)
    {
        var errors = new List<string>();

        if (!TryParseDecimal(row.Price, out var price))
        {
            errors.Add("Price is not a valid decimal number.");
        }

        if (!int.TryParse(
                row.StockQuantity,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var stockQuantity))
        {
            errors.Add("Stock quantity is not a valid integer.");
        }

        if (errors.Count > 0)
        {
            return RowValidationResult.Failure(errors.ToArray());
        }

        try
        {
            return RowValidationResult.Success(
                ProductRecord.Create(row.Sku, row.Name, price, stockQuantity));
        }
        catch (DomainValidationException exception)
        {
            return RowValidationResult.Failure(exception.Errors.ToArray());
        }
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        const NumberStyles styles = NumberStyles.Number;
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Contains(',', StringComparison.Ordinal)
            && !trimmed.Contains('.', StringComparison.Ordinal))
        {
            return decimal.TryParse(
                       trimmed,
                       styles,
                       CultureInfo.GetCultureInfo("de-DE"),
                       out result)
                   || decimal.TryParse(
                       trimmed,
                       styles,
                       CultureInfo.InvariantCulture,
                       out result);
        }

        return decimal.TryParse(
                   trimmed,
                   styles,
                   CultureInfo.InvariantCulture,
                   out result)
               || decimal.TryParse(
                   trimmed,
                   styles,
                   CultureInfo.GetCultureInfo("de-DE"),
                   out result);
    }
}

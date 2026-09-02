namespace DataBridge.Domain;

public sealed record ProductRecord
{
    private ProductRecord(string sku, string name, decimal price, int stockQuantity)
    {
        Sku = sku;
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
    }

    public string Sku { get; }
    public string Name { get; }
    public decimal Price { get; }
    public int StockQuantity { get; }

    public static ProductRecord Create(string sku, string name, decimal price, int stockQuantity)
    {
        var normalizedSku = (sku ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedName = (name ?? string.Empty).Trim();
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(normalizedSku))
        {
            errors.Add("SKU is required.");
        }
        else if (normalizedSku.Length > 50)
        {
            errors.Add("SKU must not exceed 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errors.Add("Name is required.");
        }
        else if (normalizedName.Length > 200)
        {
            errors.Add("Name must not exceed 200 characters.");
        }

        if (price < 0)
        {
            errors.Add("Price must be greater than or equal to zero.");
        }

        if (stockQuantity < 0)
        {
            errors.Add("Stock quantity must be greater than or equal to zero.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors.ToArray());
        }

        return new ProductRecord(
            normalizedSku,
            normalizedName,
            decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            stockQuantity);
    }
}

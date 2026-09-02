using DataBridge.Domain;

namespace DataBridge.UnitTests;

public sealed class ProductRecordTests
{
    [Fact]
    public void Create_NormalizesValues()
    {
        var product = ProductRecord.Create(
            "  kb-001 ",
            "  Mechanical Keyboard  ",
            129.905m,
            25);

        Assert.Equal("KB-001", product.Sku);
        Assert.Equal("Mechanical Keyboard", product.Name);
        Assert.Equal(129.91m, product.Price);
        Assert.Equal(25, product.StockQuantity);
    }

    [Fact]
    public void Create_WithMissingSku_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => ProductRecord.Create("", "Keyboard", 10m, 1));

        Assert.Contains("SKU is required.", exception.Errors);
    }

    [Fact]
    public void Create_WithNegativeValues_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => ProductRecord.Create("KB-001", "Keyboard", -1m, -2));

        Assert.Equal(2, exception.Errors.Count);
    }
}

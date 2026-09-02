using DataBridge.Application.Models;
using DataBridge.Application.Services;

namespace DataBridge.UnitTests;

public sealed class ProductRowValidatorTests
{
    private readonly ProductRowValidator _validator = new();

    [Fact]
    public void Validate_WithInvariantDecimal_ReturnsProduct()
    {
        var result = _validator.Validate(
            new RawProductRow(2, " kb-001 ", "Keyboard", "129.90", "25"));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Product);
        Assert.Equal("KB-001", result.Product.Sku);
        Assert.Equal(129.90m, result.Product.Price);
    }

    [Fact]
    public void Validate_WithGermanDecimal_ReturnsProduct()
    {
        var result = _validator.Validate(
            new RawProductRow(2, "KB-001", "Keyboard", "129,90", "25"));

        Assert.True(result.IsValid);
        Assert.Equal(129.90m, result.Product!.Price);
    }

    [Fact]
    public void Validate_WithInvalidPrice_ReturnsError()
    {
        var result = _validator.Validate(
            new RawProductRow(2, "KB-001", "Keyboard", "abc", "25"));

        Assert.False(result.IsValid);
        Assert.Contains("Price is not a valid decimal number.", result.Errors);
    }
}

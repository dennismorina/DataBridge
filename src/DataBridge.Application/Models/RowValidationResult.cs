using DataBridge.Domain;

namespace DataBridge.Application.Models;

public sealed record RowValidationResult(ProductRecord? Product, IReadOnlyCollection<string> Errors)
{
    public bool IsValid => Product is not null && Errors.Count == 0;

    public static RowValidationResult Success(ProductRecord product) =>
        new(product, Array.Empty<string>());

    public static RowValidationResult Failure(params string[] errors) =>
        new(null, errors);
}

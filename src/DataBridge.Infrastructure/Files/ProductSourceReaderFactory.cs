using DataBridge.Application.Abstractions;

namespace DataBridge.Infrastructure.Files;

public sealed class ProductSourceReaderFactory : IProductSourceReaderFactory
{
    public IProductSourceReader Create(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".csv" => new CsvProductSourceReader(),
            ".xlsx" => new ExcelProductSourceReader(),
            _ => throw new NotSupportedException(
                $"File extension '{extension}' is not supported. Use .csv or .xlsx.")
        };
    }
}

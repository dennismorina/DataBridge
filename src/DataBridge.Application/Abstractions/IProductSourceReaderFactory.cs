namespace DataBridge.Application.Abstractions;

public interface IProductSourceReaderFactory
{
    IProductSourceReader Create(string filePath);
}

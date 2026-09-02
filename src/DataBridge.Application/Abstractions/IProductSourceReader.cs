using DataBridge.Application.Models;

namespace DataBridge.Application.Abstractions;

public interface IProductSourceReader
{
    IAsyncEnumerable<RawProductRow> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

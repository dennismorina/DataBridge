using DataBridge.Application.Models;

namespace DataBridge.Application.Abstractions;

public interface IRejectWriter
{
    Task WriteAsync(
        string filePath,
        IReadOnlyCollection<RejectedRow> rows,
        CancellationToken cancellationToken = default);
}

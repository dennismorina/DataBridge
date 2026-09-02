using DataBridge.Application.Models;
using DataBridge.Domain;

namespace DataBridge.Application.Abstractions;

public interface IImportDataStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<bool> HasSuccessfulImportAsync(
        string sourceHash,
        CancellationToken cancellationToken = default);

    Task<int> UpsertBatchAsync(
        IReadOnlyCollection<ProductRecord> products,
        CancellationToken cancellationToken = default);

    Task RecordImportAsync(
        ImportJob job,
        CancellationToken cancellationToken = default);
}

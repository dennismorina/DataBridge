using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;
using DataBridge.Domain;

namespace DataBridge.Application.Services;

public sealed class DryRunImportDataStore : IImportDataStore
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> HasSuccessfulImportAsync(
        string sourceHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<int> UpsertBatchAsync(
        IReadOnlyCollection<ProductRecord> products,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(products.Count);

    public Task RecordImportAsync(
        ImportJob job,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

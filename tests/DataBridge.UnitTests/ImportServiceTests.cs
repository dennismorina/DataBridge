using DataBridge.Application.Abstractions;
using DataBridge.Application.Models;
using DataBridge.Application.Services;
using DataBridge.Domain;

namespace DataBridge.UnitTests;

public sealed class ImportServiceTests
{
    [Fact]
    public async Task ImportAsync_DuplicateSku_RejectsSecondOccurrence()
    {
        var file = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(file, "test", TestContext.Current.CancellationToken);

            var rows = new[]
            {
                new RawProductRow(2, "KB-001", "Keyboard", "10", "1"),
                new RawProductRow(3, "kb-001", "Duplicate", "20", "2")
            };

            var store = new FakeStore();
            var rejects = new FakeRejectWriter();

            var service = new ImportService(
                new FakeReaderFactory(rows),
                new ProductRowValidator(),
                rejects,
                store);

            var result = await service.ImportAsync(
                new ImportOptions(
                    file,
                    Path.Combine(Path.GetTempPath(), "databridge-rejects.csv"),
                    BatchSize: 100), TestContext.Current.CancellationToken);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.AcceptedRows);
            Assert.Equal(1, result.RejectedRows);
            Assert.Equal(1, result.UpsertedRows);
            Assert.Single(rejects.Rows);
            Assert.Contains("Duplicate SKU", rejects.Rows[0].Reason);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private sealed class FakeReaderFactory(
        IReadOnlyCollection<RawProductRow> rows) : IProductSourceReaderFactory
    {
        public IProductSourceReader Create(string filePath) => new FakeReader(rows);
    }

    private sealed class FakeReader(
        IReadOnlyCollection<RawProductRow> rows) : IProductSourceReader
    {
        public async IAsyncEnumerable<RawProductRow> ReadAsync(
            string filePath,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return row;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeStore : IImportDataStore
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

    private sealed class FakeRejectWriter : IRejectWriter
    {
        public List<RejectedRow> Rows { get; } = [];

        public Task WriteAsync(
            string filePath,
            IReadOnlyCollection<RejectedRow> rows,
            CancellationToken cancellationToken = default)
        {
            Rows.AddRange(rows);
            return Task.CompletedTask;
        }
    }
}

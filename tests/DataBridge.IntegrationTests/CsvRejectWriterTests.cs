using DataBridge.Application.Models;
using DataBridge.Infrastructure.Files;

namespace DataBridge.IntegrationTests;

public sealed class CsvRejectWriterTests
{
    [Fact]
    public async Task WriteAsync_CreatesReport()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var file = Path.Combine(directory, "rejected.csv");

        try
        {
            var writer = new CsvRejectWriter();

            await writer.WriteAsync(
                file,
                [
                    new RejectedRow(
                        4,
                        "",
                        "Missing SKU",
                        "10",
                        "1",
                        "SKU is required.")
                ], TestContext.Current.CancellationToken);

            var content = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);

            Assert.Contains("RowNumber", content);
            Assert.Contains("SKU is required.", content);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}

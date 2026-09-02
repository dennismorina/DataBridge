using DataBridge.Application.Models;
using DataBridge.Infrastructure.Files;

namespace DataBridge.IntegrationTests;

public sealed class CsvProductSourceReaderTests
{
    [Fact]
    public async Task ReadAsync_ValidCsv_ReturnsRows()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        await File.WriteAllTextAsync(
            file,
            """
            sku,name,price,stock_quantity
            KB-001,Keyboard,129.90,25
            MS-001,Mouse,59.90,50
            """, TestContext.Current.CancellationToken);

        try
        {
            var reader = new CsvProductSourceReader();
            var rows = new List<RawProductRow>();

            await foreach (var row in reader.ReadAsync(file, TestContext.Current.CancellationToken))
            {
                rows.Add(row);
            }

            Assert.Equal(2, rows.Count);
            Assert.Equal("KB-001", rows[0].Sku);
            Assert.Equal("25", rows[0].StockQuantity);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task ReadAsync_MissingHeader_Throws()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");

        await File.WriteAllTextAsync(
            file,
            """
            sku,name,price
            KB-001,Keyboard,129.90
            """, TestContext.Current.CancellationToken);

        try
        {
            var reader = new CsvProductSourceReader();

            await Assert.ThrowsAsync<InvalidDataException>(
                async () =>
                {
                    await foreach (var _ in reader.ReadAsync(file, TestContext.Current.CancellationToken))
                    {
                    }
                });
        }
        finally
        {
            File.Delete(file);
        }
    }
}

using ClosedXML.Excel;
using DataBridge.Application.Models;
using DataBridge.Infrastructure.Files;

namespace DataBridge.IntegrationTests;

public sealed class ExcelProductSourceReaderTests
{
    [Fact]
    public async Task ReadAsync_ValidWorkbook_ReturnsRows()
    {
        var file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Products");
            sheet.Cell(1, 1).Value = "sku";
            sheet.Cell(1, 2).Value = "name";
            sheet.Cell(1, 3).Value = "price";
            sheet.Cell(1, 4).Value = "stockQuantity";
            sheet.Cell(2, 1).Value = "KB-001";
            sheet.Cell(2, 2).Value = "Keyboard";
            sheet.Cell(2, 3).Value = "129.90";
            sheet.Cell(2, 4).Value = "25";
            workbook.SaveAs(file);
        }

        try
        {
            var reader = new ExcelProductSourceReader();
            var rows = new List<RawProductRow>();

            await foreach (var row in reader.ReadAsync(file, TestContext.Current.CancellationToken))
            {
                rows.Add(row);
            }

            Assert.Single(rows);
            Assert.Equal("KB-001", rows[0].Sku);
            Assert.Equal("Keyboard", rows[0].Name);
        }
        finally
        {
            File.Delete(file);
        }
    }
}

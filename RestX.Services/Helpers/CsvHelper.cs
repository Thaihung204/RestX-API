using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.BLL.Helpers
{
    public static class CsvHelper
    {
        public static void WriteHeaders(ExcelWorksheet sheet, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 73, 125));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
            }
        }
        public static void AutoFitAndStyle(ExcelWorksheet sheet, int colCount, int lastRow)
        {
            for (int r = 2; r <= lastRow; r++)
            {
                if (r % 2 == 0)
                {
                    using var range = sheet.Cells[r, 1, r, colCount];
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                }
            }
            using var tableRange = sheet.Cells[1, 1, lastRow, colCount];
            tableRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            tableRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }
        public static byte[] CreateEmptyWorkbook(string sheetName)
        {
            using var package = new ExcelPackage();
            package.Workbook.Worksheets.Add(sheetName);
            return package.GetAsByteArray();
        }
        public class CustomerStatRow
        {
            public Guid CustomerId { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime? LastVisit { get; set; }
        }
    }
}

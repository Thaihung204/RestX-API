using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.BLL.Interfaces.Reservations;
using RestX.Models.Tenants;
using System.Drawing;

namespace RestX.BLL.Services
{
    public class ExportService : BaseService, IExportService
    {
        private readonly ICustomerService customerService;
        private readonly IReservationService reservationService;
        private readonly IOrderService orderService;
        public ExportService(
            ICustomerService customerService,
            IReservationService reservationService,
            IOrderService orderService,
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null!) : base(repo, redisService, tenant)
        {
            this.customerService = customerService;
            this.reservationService = reservationService;
            this.orderService = orderService;
            ExcelPackage.License.SetNonCommercialPersonal("RestX");
        }

        public async Task<byte[]> ExportCustomersAsync(CustomerFilterParams filter)
        {
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;
            var result = await customerService.GetAllCustomers(filter);
            var customers = result.Items.ToList();

            if (!customers.Any())
                return CreateEmptyWorkbook("Customers");
            var idList = string.Join(",", customers.Select(c => $"'{c.Id}'"));
            var statsQuery = $@"
                SELECT
                    CustomerId,
                    COUNT(*) AS TotalOrders,
                    ISNULL(SUM(CASE WHEN OrderStatusId = 4 THEN TotalAmount ELSE 0 END), 0) AS TotalSpent,
                    MAX(CompletedAt) AS LastVisit
                FROM Orders
                WHERE CustomerId IN ({idList})
                GROUP BY CustomerId";

            var statsRows = await Repo.ExecuteSqlSelectAsync<CustomerStatRow>(statsQuery);
            var statsById = statsRows.ToDictionary(s => s.CustomerId);

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Customers");
            var headers = new[]
            {
                "Full Name", "Email", "Phone Number", "Membership Level",
                "Loyalty Points", "Total Orders", "Total Spent (VND)",
                "Last Visit", "Registered Date", "Status"
            };
            WriteHeaders(sheet, headers);
            int row = 2;
            foreach (var c in customers)
            {
                statsById.TryGetValue(c.Id, out var stats);
                sheet.Cells[row, 1].Value = c.FullName;
                sheet.Cells[row, 2].Value = c.Email;
                sheet.Cells[row, 3].Value = c.PhoneNumber;
                sheet.Cells[row, 4].Value = c.MembershipLevel;
                sheet.Cells[row, 5].Value = c.LoyaltyPoints;
                sheet.Cells[row, 6].Value = stats?.TotalOrders ?? 0;
                sheet.Cells[row, 7].Value = stats?.TotalSpent ?? 0;
                sheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                sheet.Cells[row, 8].Value = stats?.LastVisit.HasValue == true
                    ? stats.LastVisit.Value.ToString("dd/MM/yyyy HH:mm") : "";
                sheet.Cells[row, 9].Value = c.CreatedDate.ToString("dd/MM/yyyy");
                sheet.Cells[row, 10].Value = c.IsActive ? "Active" : "Inactive";
                row++;
            }
            AutoFitAndStyle(sheet, headers.Length, row - 1);
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportReservationsAsync(ReservationFilterParams filter)
        {
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;
            var result = await reservationService.GetReservations(filter);
            var reservations = result.Items.ToList();
            if (!reservations.Any())
                return CreateEmptyWorkbook("Reservations");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Reservations");
            var headers = new[]
            {
                "Confirmation Code", "Contact Name", "Contact Phone",
                "Reservation Date & Time", "Guests", "Tables",
                "Status", "Deposit Amount", "Created At"
            };
            WriteHeaders(sheet, headers);
            int row = 2;
            foreach (var r in reservations)
            {
                sheet.Cells[row, 1].Value = r.ConfirmationCode;
                sheet.Cells[row, 2].Value = r.ContactName;
                sheet.Cells[row, 3].Value = r.ContactPhone;
                sheet.Cells[row, 4].Value = r.ReservationDateTime.ToString("dd/MM/yyyy HH:mm");
                sheet.Cells[row, 5].Value = r.NumberOfGuests;
                sheet.Cells[row, 6].Value = string.Join("; ", r.Tables.Select(t => t.Code));
                sheet.Cells[row, 7].Value = r.Status.Name;
                sheet.Cells[row, 8].Value = r.DepositAmount;
                sheet.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                sheet.Cells[row, 9].Value = r.CreatedAt.ToString("dd/MM/yyyy HH:mm");
                row++;
            }
            AutoFitAndStyle(sheet, headers.Length, row - 1);
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportOrdersAsync(OrderSearch filter)
        {
            filter.Page = 1;
            filter.ItemsPerPage = int.MaxValue;
            var result = await orderService.GetAllOrders(filter);
            var orders = result.Orders;
            if (!orders.Any())
                return CreateEmptyWorkbook("Orders");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Orders");
            var headers = new[]
            {
                "Reference", "Order Status", "Sub Total", "Discount",
                "Tax", "Service Charge", "Total Amount", "Payment Status",
                "Item Count", "Created Date", "Completed At", "Cancelled At"
            };
            WriteHeaders(sheet, headers);
            int row = 2;
            foreach (var o in orders)
            {
                sheet.Cells[row, 1].Value = o.Reference;
                sheet.Cells[row, 2].Value = o.OrderStatusId.ToString();
                sheet.Cells[row, 3].Value = o.SubTotal ?? 0;
                sheet.Cells[row, 4].Value = o.DiscountAmount ?? 0;
                sheet.Cells[row, 5].Value = o.TaxAmount ?? 0;
                sheet.Cells[row, 6].Value = o.ServiceCharge ?? 0;
                sheet.Cells[row, 7].Value = o.TotalAmount;
                sheet.Cells[row, 8].Value = o.PaymentStatusName;
                sheet.Cells[row, 9].Value = o.OrderDetails?.Sum(d => d.Quantity) ?? 0;
                sheet.Cells[row, 10].Value = o.CreatedDate.HasValue ? o.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm") : "";
                sheet.Cells[row, 11].Value = o.CompletedAt.HasValue ? o.CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "";
                sheet.Cells[row, 12].Value = o.CancelledAt.HasValue ? o.CancelledAt.Value.ToString("dd/MM/yyyy HH:mm") : "";
                foreach (var col in new[] { 3, 4, 5, 6, 7 })
                    sheet.Cells[row, col].Style.Numberformat.Format = "#,##0";
                row++;
            }
            AutoFitAndStyle(sheet, headers.Length, row - 1);
            return package.GetAsByteArray();
        }

        #region Helpers
        private static void WriteHeaders(ExcelWorksheet sheet, string[] headers)
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

        private static void AutoFitAndStyle(ExcelWorksheet sheet, int colCount, int lastRow)
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

        private static byte[] CreateEmptyWorkbook(string sheetName)
        {
            using var package = new ExcelPackage();
            package.Workbook.Worksheets.Add(sheetName);
            return package.GetAsByteArray();
        }
        private class CustomerStatRow
        {
            public Guid CustomerId { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime? LastVisit { get; set; }
        }
        #endregion
    }
}

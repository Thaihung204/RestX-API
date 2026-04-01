using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.DataTranferObjects.Export;
using RestX.BLL.DataTranferObjects.Orders;
using RestX.BLL.DataTranferObjects.Reservation;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.BLL.Interfaces.Reservations;
using RestX.Models.Tenants;
using System.Globalization;
using System.Text;

namespace RestX.BLL.Services
{
    public class CsvExportService : BaseService, ICsvExportService
    {
        private readonly ICustomerService customerService;
        private readonly IReservationService reservationService;
        private readonly IOrderService orderService;

        public CsvExportService(
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
        }

        public async Task<byte[]> ExportCustomersCsvAsync(CustomerFilterParams filter)
        {
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;
            var result = await customerService.GetAllCustomers(filter);

            var rows = new List<CustomerExportRow>();
            foreach (var c in result.Items)
            {
                Console.WriteLine($"DEBUG NAME: {c.FullName}");
                var totalSpent = await Repo.ExecuteSqlCommandAsync<decimal>(
                    "SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders WHERE CustomerId = @CustomerId AND OrderStatusId = 4",
                    new SqlParameter("CustomerId", c.Id));

                var totalOrders = await Repo.ExecuteSqlCommandAsync<int>(
                    "SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId",
                    new SqlParameter("CustomerId", c.Id));

                var lastVisitRaw = await Repo.ExecuteSqlCommandAsync<DateTime>(
                    "SELECT ISNULL(MAX(CompletedAt), '1900-01-01') FROM Orders WHERE CustomerId = @CustomerId",
                    new SqlParameter("CustomerId", c.Id));

                rows.Add(new CustomerExportRow
                {
                    FullName = c.FullName,
                    Email = c.Email,
                    PhoneNumber = c.PhoneNumber ?? "",
                    MembershipLevel = c.MembershipLevel,
                    LoyaltyPoints = c.LoyaltyPoints,
                    TotalOrders = totalOrders,
                    TotalSpent = totalSpent,
                    LastVisit = lastVisitRaw.Year > 1900 ? lastVisitRaw.ToString("dd/MM/yyyy HH:mm") : "",
                    RegisteredDate = c.CreatedDate.ToString("dd/MM/yyyy"),
                    Status = c.IsActive ? "Active" : "Inactive"
                });
            }

            return WriteCsv(rows);
        }

        public async Task<byte[]> ExportReservationsCsvAsync(ReservationFilterParams filter)
        {
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;
            var result = await reservationService.GetReservations(filter);

            var rows = result.Items.Select(r => new ReservationExportRow
            {
                ConfirmationCode = r.ConfirmationCode,
                ContactName = r.ContactName,
                ContactPhone = r.ContactPhone ?? "",
                ReservationDateTime = r.ReservationDateTime.ToString("dd/MM/yyyy HH:mm"),
                NumberOfGuests = r.NumberOfGuests,
                Tables = string.Join("; ", r.Tables.Select(t => t.Code)),
                Status = r.Status.Name,
                DepositAmount = r.DepositAmount,
                DepositPaid = r.DepositPaid ? "Yes" : "No",
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            return WriteCsv(rows);
        }

        public async Task<byte[]> ExportOrdersCsvAsync(OrderSearch filter)
        {
            filter.Page = 1;
            filter.ItemsPerPage = int.MaxValue;
            var result = await orderService.GetAllOrders(filter);

            var rows = result.Orders.Select(o => new OrderExportRow
            {
                Reference = o.Reference ?? "",
                OrderStatus = o.OrderStatusId.ToString(),
                SubTotal = o.SubTotal ?? 0,
                DiscountAmount = o.DiscountAmount ?? 0,
                TaxAmount = o.TaxAmount ?? 0,
                ServiceCharge = o.ServiceCharge ?? 0,
                TotalAmount = o.TotalAmount,
                PaymentStatus = o.PaymentStatusName,
                ItemCount = o.OrderDetails?.Sum(d => d.Quantity) ?? 0,
                CreatedDate = o.CreatedDate.HasValue ? o.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm") : "",
                CompletedAt = o.CompletedAt.HasValue ? o.CompletedAt.Value.ToString("dd/MM/yyyy HH:mm") : "",
                CancelledAt = o.CancelledAt.HasValue ? o.CancelledAt.Value.ToString("dd/MM/yyyy HH:mm") : ""
            }).ToList();

            return WriteCsv(rows);
        }

        private static byte[] WriteCsv<T>(IEnumerable<T> records)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            };

            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms, new UTF8Encoding(true));
            using var csv = new CsvWriter(writer, config);
            csv.WriteRecords(records);
            writer.Flush();
            return ms.ToArray();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using RestX.Models.Identity;
using RestX.Models.Loyalty;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly IUserAccountService userAccountService;
        private readonly UserManager<ApplicationUser> userManager;
        private const string CustomerRole = "Customer";
        public CustomerService(
            IRepository repo,
            IRedisService redisService,
            IUserAccountService userAccountService,
            UserManager<ApplicationUser> userManager,
            IEnumerable<ActiveTenant> tenant = null!) : base(repo, redisService, tenant)
        {
            this.userAccountService = userAccountService;
            this.userManager = userManager;
        }
        public async Task<PaginatedResult<CustomerListItem>> GetAllCustomers(CustomerFilterParams filter)
        {
            var queryBuilder = new PaginationQueryBuilder(@"
                SELECT #SELECT#
                FROM Customers c
                LEFT JOIN AspNetUsers u ON c.ApplicationUserId = u.Id
                WHERE 1 = 1");
            queryBuilder
                .AddBoolCondition("c.IsActive = @IsActive", "IsActive", filter.IsActive)
                .AddLikeCondition("c.MembershipLevel = @MembershipLevel", "MembershipLevel", filter.MembershipLevel)
                .AddIntCondition("c.LoyaltyPoints >= @MinLoyaltyPoints", "MinLoyaltyPoints", filter.MinLoyaltyPoints)
                .AddIntCondition("c.LoyaltyPoints <= @MaxLoyaltyPoints", "MaxLoyaltyPoints", filter.MaxLoyaltyPoints)
                .AddSearchCondition(
                    new[] { "u.FullName", "u.Email", "u.PhoneNumber", "c.MembershipLevel" },
                    "Search",
                    filter.Search);
            var (countQuery, countParams) = queryBuilder.BuildCountQuery("COUNT(DISTINCT c.Id)");
            int totalCount = await Repo.ExecuteSqlCommandAsync<int>(countQuery, countParams);
            var selectColumns = @"DISTINCT c.Id, u.FullName, u.Email, u.PhoneNumber,
                                  c.MembershipLevel, c.LoyaltyPoints, c.IsActive, c.CreatedDate, u.AvatarUrl";
            var (dataQuery, dataParams) = queryBuilder.BuildDataQuery(
                selectColumns,
                GetSortClause(filter.SortBy, filter.SortDescending),
                filter.PageNumber,
                filter.PageSize);
            var items = await Repo.ExecuteSqlSelectAsync<CustomerListItem>(dataQuery, dataParams);
            return new PaginatedResult<CustomerListItem>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        public async Task<CustomerResponse?> GetCustomerById(Guid id)
        {
            var customer = await Repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser");
            if (customer == null) return null;
            var stats = await GetCustomerStatsAsync(id);
            return MapToResponse(customer, stats);
        }

        public async Task<CustomerResponse> CreateCustomer(CreateCustomer dto)
        {
            if (await userAccountService.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Email already exists");
            var user = await userAccountService.CreateUserAsync(new CreateUserRequest
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password,
                Role = CustomerRole,
                GenerateRandomPassword = false
            });
            var lowestBand = await GetLowestBandAsync();
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                MembershipLevel = lowestBand?.Name ?? string.Empty,
                LoyaltyPoints = lowestBand?.Min ?? 0,
                IsActive = true
            };
            try
            {
                await Repo.CreateAsync(customer);
            }
            catch
            {
                await userManager.DeleteAsync(user);
                throw;
            }

            if (dto.Avatar != null)
                await userAccountService.UploadAvatarAsync(user.Id, dto.Avatar);

            return MapToResponse(customer, user, (0, 0));
        }

        public async Task<CustomerResponse?> UpdateCustomer(Guid id, UpdateCustomer dto)
        {
            var customer = await Repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser");
            if (customer == null) return null;
            var user = customer.ApplicationUser;
            if (user != null && HasUserUpdates(dto))
            {
                await userAccountService.UpdateUserAsync(user.Id, new UpdateUserRequest
                {
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber
                });
            }

            if (user != null && dto.Avatar != null)
                await userAccountService.UploadAvatarAsync(user.Id, dto.Avatar);

            UpdateCustomerFields(customer, dto);
            Repo.Update(customer);
            await Repo.SaveAsync();

            var stats = await GetCustomerStatsAsync(id);
            return MapToResponse(customer, stats);
        }

        public async Task<bool> DeleteCustomer(Guid id)
        {
            var customer = await Repo.GetFirstAsync<Customer>(
                c => c.Id == id,
                includeProperties: "ApplicationUser");
            if (customer == null) return false;
            customer.IsActive = false;
            Repo.Update(customer);
            if (customer.ApplicationUser != null)
                await userAccountService.DeactivateUserAsync(customer.ApplicationUser);
            await Repo.SaveAsync();
            return true;
        }

        public async Task<Guid?> GetCustomerIdByApplicationUserIdAsync(Guid applicationUserId)
        {
            var customer = await Repo.GetFirstAsync<Customer>(c => c.ApplicationUserId == applicationUserId);
            return customer?.Id;
        }

        #region Private Methods
        private static string GetSortClause(string? sortBy, bool desc)
        {
            var direction = desc ? "DESC" : "ASC";
            return (sortBy?.ToLower()) switch
            {
                "fullname" => $" ORDER BY u.FullName {direction}",
                "email" => $" ORDER BY u.Email {direction}",
                "membershiplevel" => $" ORDER BY c.MembershipLevel {direction}",
                "loyaltypoints" => $" ORDER BY c.LoyaltyPoints {direction}",
                "isactive" => $" ORDER BY c.IsActive {direction}",
                "createddate" => $" ORDER BY c.CreatedDate {direction}",
                _ => $" ORDER BY c.CreatedDate {direction}"
            };
        }

        private async Task<(int TotalOrders, int TotalReservations)> GetCustomerStatsAsync(Guid customerId)
        {
            var totalOrders = await Repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", customerId));
            var totalReservations = await Repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Reservations WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", customerId));

            return (totalOrders, totalReservations);
        }

        private static void UpdateCustomerFields(Customer customer, UpdateCustomer dto)
        {
            if (!string.IsNullOrEmpty(dto.MembershipLevel))
                customer.MembershipLevel = dto.MembershipLevel;
            if (dto.LoyaltyPoints.HasValue)
                customer.LoyaltyPoints = dto.LoyaltyPoints.Value;
            if (dto.IsActive.HasValue)
                customer.IsActive = dto.IsActive.Value;
        }

        private static bool HasUserUpdates(UpdateCustomer dto)
        {
            return !string.IsNullOrWhiteSpace(dto.FullName) ||
                   !string.IsNullOrWhiteSpace(dto.PhoneNumber);
        }

        private static CustomerResponse MapToResponse(
            Customer customer,
            (int TotalOrders, int TotalReservations) stats)
        {
            return MapToResponse(customer, customer.ApplicationUser, stats);
        }

        private static CustomerResponse MapToResponse(
            Customer customer,
            ApplicationUser? user,
            (int TotalOrders, int TotalReservations) stats)
        {
            return new CustomerResponse
            {
                Id = customer.Id,
                MembershipLevel = customer.MembershipLevel,
                LoyaltyPoints = customer.LoyaltyPoints,
                IsActive = customer.IsActive,
                CreatedDate = customer.CreatedDate,
                ModifiedDate = customer.ModifiedDate,
                UserId = user?.Id ?? Guid.Empty,
                Email = user?.Email ?? string.Empty,
                FullName = user?.FullName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                AvatarUrl = user?.AvatarUrl,
                TotalOrders = stats.TotalOrders,
                TotalReservations = stats.TotalReservations
            };
        }
        #endregion

        public async Task<byte[]> ExportAsync(CustomerFilterParams filter)
        {
            ExcelPackage.License.SetNonCommercialPersonal("RestX");
            filter.PageNumber = 1;
            filter.PageSize = int.MaxValue;
            var result = await GetAllCustomers(filter);
            var customers = result.Items.ToList();

            if (!customers.Any())
                return ExcelHelper.CreateEmptyWorkbook("Customers");

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
            ExcelHelper.WriteHeaders(sheet, headers);

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

            ExcelHelper.AutoFitAndStyle(sheet, headers.Length, row - 1);
            return package.GetAsByteArray();
        }

        private class CustomerStatRow
        {
            public Guid CustomerId { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime? LastVisit { get; set; }
        }

        public async Task<Customer> CreateCustomerRecord(Guid userId)
        {
            var lowestBand = await GetLowestBandAsync();
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = userId,
                MembershipLevel = lowestBand?.Name ?? string.Empty,
                LoyaltyPoints = lowestBand?.Min ?? 0,
                IsActive = true,
                CreatedDate = DateTime.UtcNow.AddHours(7),
                ModifiedDate = DateTime.UtcNow.AddHours(7)
            };
            await Repo.CreateAsync(customer);
            return customer;
        }

        private async Task<LoyaltyPointBand?> GetLowestBandAsync()
        {
            return await Repo.GetFirstAsync<LoyaltyPointBand>(
                filter: b => b.IsActive,
                orderBy: q => q.OrderBy(b => b.Min));
        }
    }
}

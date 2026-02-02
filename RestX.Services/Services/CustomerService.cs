using Microsoft.Data.SqlClient;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Customer;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly IUserAccountService _userAccountService;
        private const string CUSTOMER_ROLE = "Customer";

        public CustomerService(
            IRepository repo,
            IRedisService redisService,
            IUserAccountService userAccountService,
            IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            _userAccountService = userAccountService;
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
                    new[] { "u.UserName", "u.Email", "u.PhoneNumber", "c.MembershipLevel" },
                    "Search",
                    filter.Search);

            var (countQuery, countParams) = queryBuilder.BuildCountQuery("COUNT(DISTINCT c.Id)");
            int totalCount = await Repo.ExecuteSqlCommandAsync<int>(countQuery, countParams);
            var selectColumns = @"DISTINCT c.Id, u.UserName AS FullName, u.Email, u.PhoneNumber,
                                  c.MembershipLevel, c.LoyaltyPoints, c.IsActive, c.CreatedDate";
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
            if (await _userAccountService.EmailExistsAsync(dto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }
            var user = await _userAccountService.CreateUserAsync(new CreateUserRequest
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Password = dto.Password,
                Role = CUSTOMER_ROLE,
                GenerateRandomPassword = false
            });
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                MembershipLevel = dto.MembershipLevel,
                LoyaltyPoints = dto.LoyaltyPoints,
                IsActive = true
            };
            await Repo.CreateAsync(customer);
            return MapToResponse(customer, user, (0, 0));
        }
        public async Task<CustomerResponse?> UpdateCustomer(Guid id, UpdateCustomer dto)
        {
            var customer = await Repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser");

            if (customer == null) return null;
            UpdateCustomerFields(customer, dto);
            var user = customer.ApplicationUser;
            if (user != null && HasUserUpdates(dto))
            {
                await _userAccountService.UpdateUserAsync(user.Id, new UpdateUserRequest
                {
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber
                });
            }
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
            {
                await _userAccountService.DeactivateUserAsync(customer.ApplicationUser);
            }
            await Repo.SaveAsync();
            return true;
        }
        #region Private Methods

        private static string GetSortClause(string? sortBy, bool desc)
        {
            var direction = desc ? "DESC" : "ASC";
            return (sortBy?.ToLower()) switch
            {
                "fullname" => $" ORDER BY u.UserName {direction}",
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
            var user = customer.ApplicationUser;
            return MapToResponse(customer, user, stats);
        }
        private static CustomerResponse MapToResponse(
            Customer customer,
            Models.Identity.ApplicationUser? user,
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
                FullName = user?.UserName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                TotalOrders = stats.TotalOrders,
                TotalReservations = stats.TotalReservations
            };
        }
        #endregion
    }
}

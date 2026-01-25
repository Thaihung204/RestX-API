using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Customer;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using RestX.Models.Identity;
using RestX.Models.Tenants;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly IRepository repo;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;

        public CustomerService(
            IRepository repo,
            IRedisService redisService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.repo = repo;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<PaginatedResult<CustomerListItemDto>> GetAllCustomers(CustomerFilterParams filter)
        {
            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM Customers c
                LEFT JOIN AspNetUsers u ON c.ApplicationUserId = u.Id
                WHERE 1 = 1
            ");

            var countParameters = new List<SqlParameter>();
            var queryParameters = new List<SqlParameter>();

            // Apply filters
            if (filter.IsActive.HasValue)
            {
                query.Append(" AND c.IsActive = @IsActive ");
                countParameters.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = filter.IsActive.Value });
                queryParameters.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = filter.IsActive.Value });
            }

            if (!string.IsNullOrEmpty(filter.MembershipLevel))
            {
                query.Append(" AND c.MembershipLevel = @MembershipLevel ");
                countParameters.Add(new SqlParameter("MembershipLevel", SqlDbType.NVarChar) { Value = filter.MembershipLevel });
                queryParameters.Add(new SqlParameter("MembershipLevel", SqlDbType.NVarChar) { Value = filter.MembershipLevel });
            }

            if (filter.MinLoyaltyPoints.HasValue)
            {
                query.Append(" AND c.LoyaltyPoints >= @MinLoyaltyPoints ");
                countParameters.Add(new SqlParameter("MinLoyaltyPoints", SqlDbType.Int) { Value = filter.MinLoyaltyPoints.Value });
                queryParameters.Add(new SqlParameter("MinLoyaltyPoints", SqlDbType.Int) { Value = filter.MinLoyaltyPoints.Value });
            }

            if (filter.MaxLoyaltyPoints.HasValue)
            {
                query.Append(" AND c.LoyaltyPoints <= @MaxLoyaltyPoints ");
                countParameters.Add(new SqlParameter("MaxLoyaltyPoints", SqlDbType.Int) { Value = filter.MaxLoyaltyPoints.Value });
                queryParameters.Add(new SqlParameter("MaxLoyaltyPoints", SqlDbType.Int) { Value = filter.MaxLoyaltyPoints.Value });
            }

            // Apply search
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query.Append(@" AND (
                    u.UserName LIKE '%' + @Search + '%'
                    OR u.Email LIKE '%' + @Search + '%'
                    OR u.PhoneNumber LIKE '%' + @Search + '%'
                    OR c.MembershipLevel LIKE '%' + @Search + '%'
                ) ");
                countParameters.Add(new SqlParameter("Search", SqlDbType.NVarChar) { Value = filter.Search });
                queryParameters.Add(new SqlParameter("Search", SqlDbType.NVarChar) { Value = filter.Search });
            }

            var countQuery = query.ToString().Replace("#SELECT#", "COUNT(DISTINCT c.Id)");
            int totalCount = await repo.ExecuteSqlCommandAsync<int>(countQuery, countParameters.ToArray());

            int skip = filter.PageNumber == 1 ? 0 : (filter.PageNumber - 1) * filter.PageSize;

            // Select columns
            var selectColumns = @"
                    DISTINCT
                     c.Id,
                     u.UserName AS FullName,
                    u.Email,
                    u.PhoneNumber,
                    c.MembershipLevel,
                    c.LoyaltyPoints,
                    c.IsActive,
                    c.CreatedDate
                     ";

            var mainQuery = query.ToString().Replace("#SELECT#", selectColumns);

            // Apply sorting
            mainQuery += GetSortClause(filter.SortBy, filter.SortDescending);

            // Apply pagination at DB level - OFFSET/FETCH
            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";

            var items = await repo.ExecuteSqlSelectAsync<CustomerListItemDto>(mainQuery, queryParameters.ToArray());

            return new PaginatedResult<CustomerListItemDto>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        private string GetSortClause(string? sortBy, bool desc)
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

        public async Task<CustomerResponseDto?> GetCustomerById(Guid id)
        {
            var customer = await repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser");

            if (customer == null) return null;

            var totalOrders = await repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", id));

            var totalReservations = await repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Reservations WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", id));

            var user = customer.ApplicationUser;

            return new CustomerResponseDto
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
                TotalOrders = totalOrders,
                TotalReservations = totalReservations
            };
        }


        public async Task<CustomerResponseDto> CreateCustomer(CreateCustomerDto dto)
        {
            var existingUser = await userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow,
                RefreshToken = string.Empty
            };

            var createResult = await userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            const string customerRole = "Customer";
            if (!await roleManager.RoleExistsAsync(customerRole))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(customerRole));
            }
            await userManager.AddToRoleAsync(user, customerRole);

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                MembershipLevel = dto.MembershipLevel,
                LoyaltyPoints = dto.LoyaltyPoints,
                IsActive = true
            };

            await repo.CreateAsync(customer);
            return new CustomerResponseDto
            {
                Id = customer.Id,
                MembershipLevel = customer.MembershipLevel,
                LoyaltyPoints = customer.LoyaltyPoints,
                IsActive = customer.IsActive,
                CreatedDate = customer.CreatedDate,
                ModifiedDate = customer.ModifiedDate,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = dto.FullName,
                PhoneNumber = user.PhoneNumber,
                TotalOrders = 0,
                TotalReservations = 0
            };
        }

        public async Task<CustomerResponseDto?> UpdateCustomer(Guid id, UpdateCustomerDto dto)
        {
            var customer = await repo.GetFirstAsync<Customer>(
       filter: c => c.Id == id,
       includeProperties: "ApplicationUser"
   );

            if (customer == null) return null;

            if (!string.IsNullOrEmpty(dto.MembershipLevel)) customer.MembershipLevel = dto.MembershipLevel;
            if (dto.LoyaltyPoints.HasValue) customer.LoyaltyPoints = dto.LoyaltyPoints.Value;
            if (dto.IsActive.HasValue) customer.IsActive = dto.IsActive.Value;

            repo.Update(customer);
            await repo.SaveAsync();

            var user = customer.ApplicationUser;
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.FullName))
                    user.UserName = dto.FullName;

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    user.PhoneNumber = dto.PhoneNumber;

                user.LastModified = DateTime.UtcNow;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to update user: {errors}");
                }
            }
            var totalOrders = await repo.ExecuteSqlCommandAsync<int>(
        "SELECT COUNT(*) FROM Orders WHERE CustomerId = @CustomerId",
        new SqlParameter("CustomerId", id)
    );

            var totalReservations = await repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Reservations WHERE CustomerId = @CustomerId",
                new SqlParameter("CustomerId", id)
            );

            return new CustomerResponseDto
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
                TotalOrders = totalOrders,
                TotalReservations = totalReservations
            };
        }

        public async Task<bool> DeleteCustomer(Guid id)
        {

            var customer = await repo.GetFirstAsync<Customer>(
                c => c.Id == id,
                includeProperties: "ApplicationUser"
            );

            if (customer == null)
                return false;

            customer.IsActive = false;
            repo.Update(customer);

            var user = customer.ApplicationUser;
            if (user != null)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.LastModified = DateTime.UtcNow;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(errors);
                }
            }

            await repo.SaveAsync();

            return true;
        }

    }
}

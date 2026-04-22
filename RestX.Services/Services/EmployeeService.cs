using Microsoft.Extensions.Logging;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.DataTranferObjects.Employee;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Auth;
using RestX.BLL.Interfaces.Employees;
using RestX.Models.Enum;
using RestX.Models.HR;
using RestX.Models.Identity;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly IUserAccountService userAccountService;
        private readonly IAuthLinkService authLinkService;
        private readonly IEmailService emailService;
        private readonly ILogger<EmployeeService>? logger;

        public EmployeeService(
            IRepository repo,
            IRedisService redisService,
            IUserAccountService userAccountService,
            IAuthLinkService authLinkService,
            IEmailService emailService,
            ILogger<EmployeeService>? logger = null,
            IEnumerable<ActiveTenant> tenant = null!) : base(repo, redisService, tenant)
        {
            this.userAccountService = userAccountService;
            this.authLinkService = authLinkService;
            this.emailService = emailService;
            this.logger = logger;
        }

        public async Task<PaginatedResult<EmployeeListItem>> GetAllEmployeesPaginated(EmployeeFilterParams filter)
        {
            var queryBuilder = new PaginationQueryBuilder(@"
                SELECT #SELECT#
                FROM Employees e
                LEFT JOIN AspNetUsers u ON e.Id = u.MemberId
                WHERE 1 = 1");

            queryBuilder
                .AddBoolCondition("e.IsActive = @IsActive", "IsActive", filter.IsActive)
                .AddLikeCondition("e.Position LIKE '%' + @Position + '%'", "Position", filter.Position)
                .AddDateCondition("e.HireDate >= @HireDateFrom", "HireDateFrom", filter.HireDateFrom)
                .AddDateCondition("e.HireDate <= @HireDateTo", "HireDateTo", filter.HireDateTo)
                .AddSearchCondition(
                    new[] { "e.Code", "e.Position", "u.FullName", "u.Email" },
                    "Search",
                    filter.Search);
            var (countQuery, countParams) = queryBuilder.BuildCountQuery("COUNT(DISTINCT e.Id)");
            int totalCount = await Repo.ExecuteSqlCommandAsync<int>(countQuery, countParams);

            int totalActive = await Repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Employees WHERE IsActive = 1");
            int totalInactive = await Repo.ExecuteSqlCommandAsync<int>(
                "SELECT COUNT(*) FROM Employees WHERE IsActive = 0");

            var selectColumns = @"DISTINCT e.Id, e.Code, u.FullName, u.Email,
                                  e.Position, e.IsActive, e.HireDate, e.CreatedDate, u.AvatarUrl";
            var (dataQuery, dataParams) = queryBuilder.BuildDataQuery(
                selectColumns,
                GetSortClause(filter.SortBy, filter.SortDescending),
                filter.PageNumber,
                filter.PageSize);

            var items = await Repo.ExecuteSqlSelectAsync<EmployeeListItem>(dataQuery, dataParams);
            return new PaginatedResult<EmployeeListItem>(items, totalCount, filter.PageNumber, filter.PageSize)
            {
                TotalActive = totalActive,
                TotalInactive = totalInactive
            };
        }

        public async Task<EmployeeResponse?> GetEmployeeById(Guid id)
        {
            var employee = await Repo.GetFirstAsync<Employee>(e => e.Id == id);
            if (employee == null) return null;

            var user = await userAccountService.GetUserByMemberIdAsync(id);
            var roles = user != null
                ? await userAccountService.GetUserRolesAsync(user)
                : new List<string>();

            return MapToResponse(employee, user, roles);
        }

        public async Task<EmployeeResponse> CreateEmployee(CreateEmployee dto)
        {
            if (await userAccountService.EmailExistsAsync(dto.Email))
                throw new InvalidOperationException("Email already exists");
            var employee = await CreateEmployeeEntityAsync(dto);
            await Repo.CreateAsync(employee);

            ApplicationUser user;
            try
            {
                user = await userAccountService.CreateUserAsync(new CreateUserRequest
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Role = dto.Role,
                    MemberId = employee.Id,
                    GenerateRandomPassword = true
                });
            }
            catch
            {
                Repo.Delete<Employee>(employee);
                await Repo.SaveAsync();
                throw;
            }
            if (dto.Avatar != null)
                await userAccountService.UploadAvatarAsync(user.Id, dto.Avatar);
            await TrySendWelcomeEmailAsync(user, dto.FullName);
            var roles = await userAccountService.GetUserRolesAsync(user);
            return MapToResponse(employee, user, roles);
        }

        public async Task<EmployeeResponse?> UpdateEmployee(Guid id, UpdateEmployee dto)
        {
            var employee = await Repo.GetFirstAsync<Employee>(filter: e => e.Id == id);
            if (employee == null) return null;
            var user = await userAccountService.GetUserByMemberIdAsync(id);
            if (user != null && HasUserUpdates(dto))
            {
                user = await userAccountService.UpdateUserAsync(user.Id, new UpdateUserRequest
                {
                    FullName = dto.FullName,
                    PhoneNumber = dto.PhoneNumber
                });
            }
            if (user != null && dto.Avatar != null)
                await userAccountService.UploadAvatarAsync(user.Id, dto.Avatar);

            UpdateEmployeeFields(employee, dto);
            Repo.Update(employee);
            await Repo.SaveAsync();

            var roles = user != null
                ? await userAccountService.GetUserRolesAsync(user)
                : new List<string>();

            return MapToResponse(employee, user, roles);
        }

        public async Task<bool> DeleteEmployee(Guid id)
        {
            var employee = await Repo.GetFirstAsync<Employee>(
                filter: e => e.Id == id,
                includeProperties: "ApplicationUser");

            if (employee == null) return false;

            var activeOrderCount = await Repo.GetCountAsync<RestX.Models.Orders.Order>(
                o => o.HandledBy == id && o.OrderStatusId == (int)OrderStatus.Open);
            if (activeOrderCount > 0)
                throw new InvalidOperationException("Cannot deactivate employee with open orders");

            var pendingPaymentCount = await Repo.GetCountAsync<RestX.Models.Orders.Payment>(
                p => p.ProcessedBy == id && p.Status == PaymentStatus.Pending);
            if (pendingPaymentCount > 0)
                throw new InvalidOperationException("Cannot deactivate employee with pending payments");

            employee.IsActive = false;
            employee.TerminationDate = DateTime.UtcNow.AddHours(7);
            Repo.Update(employee);

            if (employee.ApplicationUser != null)
                await userAccountService.DeactivateUserAsync(employee.ApplicationUser);

            await Repo.SaveAsync();
            return true;
        }

        #region Private Methods

        private static string GetSortClause(string? sortBy, bool sortDescending)
        {
            var direction = sortDescending ? "DESC" : "ASC";
            return (sortBy?.ToLower()) switch
            {
                "code" => $" ORDER BY e.Code {direction}",
                "fullname" => $" ORDER BY u.FullName {direction}",
                "email" => $" ORDER BY u.Email {direction}",
                "position" => $" ORDER BY e.Position {direction}",
                "hiredate" => $" ORDER BY e.HireDate {direction}",
                "isactive" => $" ORDER BY e.IsActive {direction}",
                _ => $" ORDER BY e.CreatedDate {direction}"
            };
        }

        private async Task<Employee> CreateEmployeeEntityAsync(CreateEmployee dto)
        {
            var code = await GenerateEmployeeCodeAsync();

            return new Employee
            {
                Id = Guid.NewGuid(),
                Code = code,
                Address = dto.Address,
                Position = dto.Position,
                HireDate = dto.HireDate,
                Salary = 0,
                SalaryType = "Monthly",
                IsActive = true
            };
        }

        private async Task<string> GenerateEmployeeCodeAsync()
        {
            const string prefix = "RTX";
            var query = @"SELECT ISNULL(MAX(Code), '') FROM Employees WHERE Code LIKE 'RTX%'";
            var lastCode = await Repo.ExecuteSqlCommandAsync<string>(query);

            if (string.IsNullOrWhiteSpace(lastCode))
                return "RTX001";

            var numberPart = lastCode[prefix.Length..];
            if (!int.TryParse(numberPart, out int lastNumber))
                throw new InvalidOperationException("Invalid employee code format in database.");

            return $"{prefix}{(lastNumber + 1):D3}";
        }

        private async Task TrySendWelcomeEmailAsync(ApplicationUser user, string fullName)
        {
            try
            {
                var baseUrl = GetTenantBaseUrl();
                var welcomeLink = await authLinkService.GenerateWelcomeLinkAsync(user, baseUrl);
                await emailService.SendWelcomeEmployeeAsync(user.Email!, fullName, welcomeLink);
            }
            catch (Exception ex)
            {
                // Log but don't fail - employee is already created, email can be resent later
                logger?.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
            }
        }

        private string GetTenantBaseUrl()
            => $"https://{CurrentTenant?.Hostname ?? "localhost"}";

        private static void UpdateEmployeeFields(Employee employee, UpdateEmployee dto)
        {
            if (!string.IsNullOrEmpty(dto.Code))
                employee.Code = dto.Code;
            if (dto.Address != null)
                employee.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.Position))
                employee.Position = dto.Position;
            if (dto.HireDate.HasValue)
                employee.HireDate = dto.HireDate.Value;
            if (dto.TerminationDate.HasValue)
                employee.TerminationDate = dto.TerminationDate;
            if (dto.IsActive.HasValue)
                employee.IsActive = dto.IsActive.Value;
        }

        private static bool HasUserUpdates(UpdateEmployee dto)
        {
            return !string.IsNullOrEmpty(dto.FullName) ||
                   !string.IsNullOrEmpty(dto.PhoneNumber);
        }

        private static EmployeeResponse MapToResponse(
            Employee employee,
            ApplicationUser? user,
            IList<string> roles)
        {
            return new EmployeeResponse
            {
                Id = employee.Id,
                Code = employee.Code,
                Address = employee.Address,
                Position = employee.Position,
                HireDate = employee.HireDate,
                TerminationDate = employee.TerminationDate,
                Salary = employee.Salary,
                SalaryType = employee.SalaryType,
                IsActive = employee.IsActive,
                CreatedDate = employee.CreatedDate,
                ModifiedDate = employee.ModifiedDate,
                UserId = user?.Id ?? Guid.Empty,
                Email = user?.Email ?? string.Empty,
                FullName = user?.FullName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                AvatarUrl = user?.AvatarUrl,
                Roles = roles.ToList()
            };
        }

        #endregion
    }
}

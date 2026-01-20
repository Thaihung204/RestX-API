using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Employee;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Employees;
using RestX.Models.HR;
using RestX.Models.Identity;
using System.Data;
using System.Text;

namespace RestX.BLL.Services
{
    public class EmployeeService : BaseService, IEmployeeService
    {
        private readonly IRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public EmployeeService(
            IRepository repo,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager) : base(repo)
        {
            _repo = repo;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PaginatedResult<EmployeeListItemDto>> GetAllEmployeesPaginated(EmployeeFilterParams filter)
        {
            var query = new StringBuilder();
            query.Append(@"
                SELECT #SELECT#
                FROM Employees e
                LEFT JOIN AspNetUsers u ON e.Id = u.MemberId
                WHERE 1 = 1
            ");

            var countParameters = new List<SqlParameter>();
            var queryParameters = new List<SqlParameter>();

            // Apply filters
            if (filter.IsActive.HasValue)
            {
                query.Append(" AND e.IsActive = @IsActive ");
                countParameters.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = filter.IsActive.Value });
                queryParameters.Add(new SqlParameter("IsActive", SqlDbType.Bit) { Value = filter.IsActive.Value });
            }

            if (!string.IsNullOrEmpty(filter.Position))
            {
                query.Append(" AND e.Position LIKE '%' + @Position + '%' ");
                countParameters.Add(new SqlParameter("Position", SqlDbType.NVarChar) { Value = filter.Position });
                queryParameters.Add(new SqlParameter("Position", SqlDbType.NVarChar) { Value = filter.Position });
            }

            if (filter.HireDateFrom.HasValue)
            {
                query.Append(" AND e.HireDate >= @HireDateFrom ");
                countParameters.Add(new SqlParameter("HireDateFrom", SqlDbType.DateTime) { Value = filter.HireDateFrom.Value });
                queryParameters.Add(new SqlParameter("HireDateFrom", SqlDbType.DateTime) { Value = filter.HireDateFrom.Value });
            }

            if (filter.HireDateTo.HasValue)
            {
                query.Append(" AND e.HireDate <= @HireDateTo ");
                countParameters.Add(new SqlParameter("HireDateTo", SqlDbType.DateTime) { Value = filter.HireDateTo.Value });
                queryParameters.Add(new SqlParameter("HireDateTo", SqlDbType.DateTime) { Value = filter.HireDateTo.Value });
            }

            // Apply search
            if (!string.IsNullOrEmpty(filter.Search))
            {
                query.Append(@" AND (
                    e.Code LIKE '%' + @Search + '%'
                    OR e.Position LIKE '%' + @Search + '%'
                    OR u.UserName LIKE '%' + @Search + '%'
                    OR u.Email LIKE '%' + @Search + '%'
                ) ");
                countParameters.Add(new SqlParameter("Search", SqlDbType.NVarChar) { Value = filter.Search });
                queryParameters.Add(new SqlParameter("Search", SqlDbType.NVarChar) { Value = filter.Search });
            }

            // Count query - execute at DB level
            var countQuery = query.ToString().Replace("#SELECT#", "COUNT(*)");
            var totalCount = await _repo.ExecuteSqlCommandAsync<int>(countQuery, countParameters.ToArray());

            // Pagination calculation
            int skip = (filter.PageNumber - 1) * filter.PageSize;

            // Select columns
            var selectColumns = @"
                e.Id,
                e.Code,
                e.Position,
                e.IsActive,
                e.HireDate,
                u.Email,
                u.UserName AS FullName
            ";

            var mainQuery = query.ToString().Replace("#SELECT#", selectColumns);

            // Apply sorting
            mainQuery += GetSortClause(filter.SortBy, filter.SortDescending);

            // Apply pagination at DB level - OFFSET/FETCH
            mainQuery += $" OFFSET {skip} ROWS FETCH NEXT {filter.PageSize} ROWS ONLY";

            var items = await _repo.ExecuteSqlSelectAsync<EmployeeListItemDto>(mainQuery, queryParameters.ToArray());

            return new PaginatedResult<EmployeeListItemDto>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        private static string GetSortClause(string? sortBy, bool sortDescending)
        {
            var direction = sortDescending ? "DESC" : "ASC";

            return (sortBy?.ToLower()) switch
            {
                "code" => $" ORDER BY e.Code {direction}",
                "fullname" => $" ORDER BY u.UserName {direction}",
                "email" => $" ORDER BY u.Email {direction}",
                "position" => $" ORDER BY e.Position {direction}",
                "hiredate" => $" ORDER BY e.HireDate {direction}",
                "isactive" => $" ORDER BY e.IsActive {direction}",
                _ => $" ORDER BY e.CreatedDate {direction}"
            };
        }

        public async Task<EmployeeResponseDto?> GetEmployeeById(Guid id)
        {
            var employee = await _repo.GetFirstAsync<Employee>(filter: e => e.Id == id);

            if (employee == null) return null;

            var user = _userManager.Users.FirstOrDefault(u => u.MemberId == id);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            return new EmployeeResponseDto
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
                FullName = user?.UserName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                Roles = roles.ToList()
            };
        }

        public async Task<EmployeeResponseDto> CreateEmployee(CreateEmployeeDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            var existingEmployee = await _repo.GetFirstAsync<Employee>(filter: e => e.Code == dto.Code);
            if (existingEmployee != null)
            {
                throw new InvalidOperationException("Employee code already exists");
            }

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Address = dto.Address,
                Position = dto.Position,
                HireDate = dto.HireDate,
                Salary = dto.Salary,
                SalaryType = dto.SalaryType,
                IsActive = true
            };

            await _repo.CreateAsync(employee);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow,
                MemberId = employee.Id,
                RefreshToken = string.Empty
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                _repo.Delete<Employee>(employee);
                await _repo.SaveAsync();

                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(dto.Role));
            }
            await _userManager.AddToRoleAsync(user, dto.Role);

            var roles = await _userManager.GetRolesAsync(user);

            return new EmployeeResponseDto
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
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.ToList()
            };
        }

        public async Task<EmployeeResponseDto?> UpdateEmployee(Guid id, UpdateEmployeeDto dto)
        {
            var employee = await _repo.GetFirstAsync<Employee>(filter: e => e.Id == id);

            if (employee == null) return null;

            if (!string.IsNullOrEmpty(dto.Code)) employee.Code = dto.Code;
            if (dto.Address != null) employee.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.Position)) employee.Position = dto.Position;
            if (dto.HireDate.HasValue) employee.HireDate = dto.HireDate.Value;
            if (dto.TerminationDate.HasValue) employee.TerminationDate = dto.TerminationDate;
            if (dto.Salary.HasValue) employee.Salary = dto.Salary.Value;
            if (!string.IsNullOrEmpty(dto.SalaryType)) employee.SalaryType = dto.SalaryType;
            if (dto.IsActive.HasValue) employee.IsActive = dto.IsActive.Value;

            _repo.Update(employee);
            await _repo.SaveAsync();

            var user = _userManager.Users.FirstOrDefault(u => u.MemberId == id);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(dto.FullName)) user.UserName = dto.FullName;
                if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
                user.LastModified = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            return new EmployeeResponseDto
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
                FullName = user?.UserName ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                Roles = roles.ToList()
            };
        }

        public async Task<bool> DeleteEmployee(Guid id)
        {
            var employee = await _repo.GetFirstAsync<Employee>(filter: e => e.Id == id);

            if (employee == null) return false;

            employee.IsActive = false;
            employee.TerminationDate = DateTime.UtcNow;

            _repo.Update(employee);
            await _repo.SaveAsync();

            return true;
        }
    }
}

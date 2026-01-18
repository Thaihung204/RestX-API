using Microsoft.AspNetCore.Identity;
using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Employee;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Employees;
using RestX.Models.HR;
using RestX.Models.Identity;
using System.Linq.Expressions;

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
            Expression<Func<Employee, bool>>? filterExpression = null;
            var predicates = new List<Expression<Func<Employee, bool>>>();

            if (filter.IsActive.HasValue)
            {
                predicates.Add(e => e.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(filter.Position))
            {
                predicates.Add(e => e.Position.Contains(filter.Position));
            }

            if (filter.HireDateFrom.HasValue)
            {
                predicates.Add(e => e.HireDate >= filter.HireDateFrom.Value);
            }
            if (filter.HireDateTo.HasValue)
            {
                predicates.Add(e => e.HireDate <= filter.HireDateTo.Value);
            }

            if (predicates.Any())
            {
                filterExpression = predicates.Aggregate((current, next) => CombineExpressions(current, next));
            }

            var allEmployees = await _repo.GetAsync<Employee>(filter: filterExpression);

            var employeeIds = allEmployees.Select(e => e.Id).ToList();
            var users = _userManager.Users.Where(u => u.MemberId.HasValue && employeeIds.Contains(u.MemberId.Value)).ToList();
            var userDict = users.Where(u => u.MemberId.HasValue).ToDictionary(u => u.MemberId!.Value, u => u);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                allEmployees = allEmployees.Where(e =>
                    e.Code.ToLower().Contains(searchLower) ||
                    e.Position.ToLower().Contains(searchLower) ||
                    (userDict.TryGetValue(e.Id, out var user) && (
                        (user.UserName?.ToLower().Contains(searchLower) ?? false) ||
                        (user.Email?.ToLower().Contains(searchLower) ?? false)
                    ))
                ).ToList();
            }

            allEmployees = ApplySorting(allEmployees, userDict, filter.SortBy, filter.SortDescending);

            var totalCount = allEmployees.Count();

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var employees = allEmployees.Skip(skip).Take(filter.PageSize);

            var items = employees.Select(e =>
            {
                userDict.TryGetValue(e.Id, out var user);
                return new EmployeeListItemDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    FullName = user?.UserName ?? string.Empty,
                    Email = user?.Email ?? string.Empty,
                    Position = e.Position,
                    IsActive = e.IsActive,
                    HireDate = e.HireDate
                };
            }).ToList();

            return new PaginatedResult<EmployeeListItemDto>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        private IEnumerable<Employee> ApplySorting(IEnumerable<Employee> employees, Dictionary<Guid, ApplicationUser> userDict, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return sortDescending
                    ? employees.OrderByDescending(e => e.CreatedDate)
                    : employees.OrderBy(e => e.CreatedDate);
            }

            return sortBy.ToLower() switch
            {
                "code" => sortDescending ? employees.OrderByDescending(e => e.Code) : employees.OrderBy(e => e.Code),
                "fullname" => sortDescending
                    ? employees.OrderByDescending(e => userDict.TryGetValue(e.Id, out var u) ? u.UserName : "")
                    : employees.OrderBy(e => userDict.TryGetValue(e.Id, out var u) ? u.UserName : ""),
                "email" => sortDescending
                    ? employees.OrderByDescending(e => userDict.TryGetValue(e.Id, out var u) ? u.Email : "")
                    : employees.OrderBy(e => userDict.TryGetValue(e.Id, out var u) ? u.Email : ""),
                "position" => sortDescending ? employees.OrderByDescending(e => e.Position) : employees.OrderBy(e => e.Position),
                "hiredate" => sortDescending ? employees.OrderByDescending(e => e.HireDate) : employees.OrderBy(e => e.HireDate),
                "isactive" => sortDescending ? employees.OrderByDescending(e => e.IsActive) : employees.OrderBy(e => e.IsActive),
                _ => sortDescending ? employees.OrderByDescending(e => e.CreatedDate) : employees.OrderBy(e => e.CreatedDate)
            };
        }

        private static Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));
            var combined = Expression.AndAlso(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter)
            );
            return Expression.Lambda<Func<T, bool>>(combined, parameter);
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

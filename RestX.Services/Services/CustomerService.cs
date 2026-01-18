using Microsoft.AspNetCore.Identity;
using RestX.BLL.DTOs.Common;
using RestX.BLL.DTOs.Customer;
using RestX.BLL.Interfaces;
using RestX.BLL.Interfaces.Customers;
using RestX.Models.Customers;
using RestX.Models.Identity;
using System.Linq.Expressions;

namespace RestX.BLL.Services
{
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly IRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public CustomerService(
            IRepository repo,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager) : base(repo)
        {
            _repo = repo;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<PaginatedResult<CustomerListItemDto>> GetAllCustomers(CustomerFilterParams filter)
        {
            Expression<Func<Customer, bool>>? filterExpression = null;
            var predicates = new List<Expression<Func<Customer, bool>>>();

            if (filter.IsActive.HasValue)
            {
                predicates.Add(c => c.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrEmpty(filter.MembershipLevel))
            {
                predicates.Add(c => c.MembershipLevel == filter.MembershipLevel);
            }

            if (filter.MinLoyaltyPoints.HasValue)
            {
                predicates.Add(c => c.LoyaltyPoints >= filter.MinLoyaltyPoints.Value);
            }
            if (filter.MaxLoyaltyPoints.HasValue)
            {
                predicates.Add(c => c.LoyaltyPoints <= filter.MaxLoyaltyPoints.Value);
            }

            if (predicates.Any())
            {
                filterExpression = predicates.Aggregate((current, next) => CombineExpressions(current, next));
            }

            var allCustomers = await _repo.GetAsync<Customer>(
                filter: filterExpression,
                includeProperties: "ApplicationUser"
            );

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                allCustomers = allCustomers.Where(c =>
                    (c.ApplicationUser?.UserName?.ToLower().Contains(searchLower) ?? false) ||
                    (c.ApplicationUser?.Email?.ToLower().Contains(searchLower) ?? false) ||
                    (c.ApplicationUser?.PhoneNumber?.Contains(searchLower) ?? false) ||
                    c.MembershipLevel.ToLower().Contains(searchLower)
                ).ToList();
            }

            allCustomers = ApplySorting(allCustomers, filter.SortBy, filter.SortDescending);

            var totalCount = allCustomers.Count();

            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var customers = allCustomers.Skip(skip).Take(filter.PageSize);

            var items = customers.Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                Email = c.ApplicationUser?.Email ?? string.Empty,
                FullName = c.ApplicationUser?.UserName ?? string.Empty,
                PhoneNumber = c.ApplicationUser?.PhoneNumber,
                MembershipLevel = c.MembershipLevel,
                LoyaltyPoints = c.LoyaltyPoints,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate
            }).ToList();

            return new PaginatedResult<CustomerListItemDto>(items, totalCount, filter.PageNumber, filter.PageSize);
        }

        private IEnumerable<Customer> ApplySorting(IEnumerable<Customer> customers, string? sortBy, bool sortDescending)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return sortDescending
                    ? customers.OrderByDescending(c => c.CreatedDate)
                    : customers.OrderBy(c => c.CreatedDate);
            }

            return sortBy.ToLower() switch
            {
                "fullname" => sortDescending ? customers.OrderByDescending(c => c.ApplicationUser?.UserName) : customers.OrderBy(c => c.ApplicationUser?.UserName),
                "email" => sortDescending ? customers.OrderByDescending(c => c.ApplicationUser?.Email) : customers.OrderBy(c => c.ApplicationUser?.Email),
                "membershiplevel" => sortDescending ? customers.OrderByDescending(c => c.MembershipLevel) : customers.OrderBy(c => c.MembershipLevel),
                "loyaltypoints" => sortDescending ? customers.OrderByDescending(c => c.LoyaltyPoints) : customers.OrderBy(c => c.LoyaltyPoints),
                "isactive" => sortDescending ? customers.OrderByDescending(c => c.IsActive) : customers.OrderBy(c => c.IsActive),
                "createddate" => sortDescending ? customers.OrderByDescending(c => c.CreatedDate) : customers.OrderBy(c => c.CreatedDate),
                _ => sortDescending ? customers.OrderByDescending(c => c.CreatedDate) : customers.OrderBy(c => c.CreatedDate)
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

        public async Task<CustomerResponseDto?> GetCustomerById(Guid id)
        {
            var customer = await _repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser,Orders,Reservations"
            );

            if (customer == null) return null;

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
                TotalOrders = customer.Orders?.Count ?? 0,
                TotalReservations = customer.Reservations?.Count ?? 0
            };
        }

        public async Task<CustomerResponseDto> CreateCustomer(CreateCustomerDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = dto.FullName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            const string customerRole = "Customer";
            if (!await _roleManager.RoleExistsAsync(customerRole))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(customerRole));
            }
            await _userManager.AddToRoleAsync(user, customerRole);

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = user.Id,
                MembershipLevel = dto.MembershipLevel,
                LoyaltyPoints = dto.LoyaltyPoints,
                IsActive = true
            };

            await _repo.CreateAsync(customer);

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
                FullName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                TotalOrders = 0,
                TotalReservations = 0
            };
        }

        public async Task<CustomerResponseDto?> UpdateCustomer(Guid id, UpdateCustomerDto dto)
        {
            var customer = await _repo.GetFirstAsync<Customer>(
                filter: c => c.Id == id,
                includeProperties: "ApplicationUser,Orders,Reservations"
            );

            if (customer == null) return null;

            if (!string.IsNullOrEmpty(dto.MembershipLevel)) customer.MembershipLevel = dto.MembershipLevel;
            if (dto.LoyaltyPoints.HasValue) customer.LoyaltyPoints = dto.LoyaltyPoints.Value;
            if (dto.IsActive.HasValue) customer.IsActive = dto.IsActive.Value;

            _repo.Update(customer);
            await _repo.SaveAsync();

            var user = customer.ApplicationUser;
            if (user != null)
            {
                if (!string.IsNullOrEmpty(dto.FullName)) user.UserName = dto.FullName;
                if (!string.IsNullOrEmpty(dto.PhoneNumber)) user.PhoneNumber = dto.PhoneNumber;
                user.LastModified = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

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
                TotalOrders = customer.Orders?.Count ?? 0,
                TotalReservations = customer.Reservations?.Count ?? 0
            };
        }

        public async Task<bool> DeleteCustomer(Guid id)
        {
            var customer = await _repo.GetByIdAsync<Customer>(id);

            if (customer == null) return false;

            customer.IsActive = false;

            _repo.Update(customer);
            await _repo.SaveAsync();

            return true;
        }
    }
}

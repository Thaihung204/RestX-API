using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces;
using RestX.Models.Identity;

namespace RestX.BLL.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        public UserAccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<ApplicationUser> CreateUserAsync(CreateUserRequest request)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                EmailConfirmed = true,
                LastModified = DateTime.UtcNow,
                MemberId = request.MemberId,
                RefreshToken = string.Empty
            };
            var password = request.GenerateRandomPassword
                ? GenerateRandomPassword()
                : request.Password ?? throw new ArgumentException("Password is required");

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {GetErrors(result)}");
            }
            await EnsureRoleAndAssignAsync(user, request.Role);
            return user;
        }

        public async Task<ApplicationUser> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found");
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.UserName = request.FullName;
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            user.LastModified = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to update user: {GetErrors(result)}");
            }
            return user;
        }
        public async Task DeactivateUserAsync(ApplicationUser user)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LastModified = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to deactivate user: {GetErrors(result)}");
            }
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
          }
        public async Task<ApplicationUser?> GetUserByMemberIdAsync(Guid memberId)
        {
            return await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MemberId == memberId);
        }
        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }
        public async Task<string> GenerateWelcomeLinkAsync(ApplicationUser user, string baseUrl)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedEmail = Uri.EscapeDataString(user.Email!);
            var encodedToken = Uri.EscapeDataString(token);
            return $"{baseUrl}/set-password?email={encodedEmail}&token={encodedToken}";
        }
        private async Task EnsureRoleAndAssignAsync(ApplicationUser user, string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
            await _userManager.AddToRoleAsync(user, roleName);
        }
        private static string GenerateRandomPassword()
        {
            return $"Rx@{Guid.NewGuid():N}"[..12];
        }
        private static string GetErrors(IdentityResult result)
        {
            return string.Join(", ", result.Errors.Select(e => e.Description));
        }
    }
}

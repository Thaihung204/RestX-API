using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.BLL.DataTranferObjects.Common;
using RestX.BLL.Interfaces.Auth;
using RestX.Models.Identity;

namespace RestX.BLL.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole<Guid>> roleManager;

        public UserAccountService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
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

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user: {FormatIdentityErrors(result)}");

            await EnsureRoleAndAssignAsync(user, request.Role);
            return user;
        }

        public async Task<ApplicationUser> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await userManager.FindByIdAsync(userId.ToString())
                ?? throw new InvalidOperationException("User not found");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.UserName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;

            user.LastModified = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to update user: {FormatIdentityErrors(result)}");

            return user;
        }

        public async Task DeactivateUserAsync(ApplicationUser user)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LastModified = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to deactivate user: {FormatIdentityErrors(result)}");
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            return user != null;
        }

        public async Task<ApplicationUser?> GetUserByMemberIdAsync(Guid memberId)
        {
            return await userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.MemberId == memberId);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await userManager.GetRolesAsync(user);
        }

        private async Task EnsureRoleAndAssignAsync(ApplicationUser user, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));

            await userManager.AddToRoleAsync(user, roleName);
        }

        private static string FormatIdentityErrors(IdentityResult result)
            => string.Join(", ", result.Errors.Select(e => e.Description));

        private static string GenerateRandomPassword()
            => $"Rx@{Guid.NewGuid():N}"[..12];
    }
}

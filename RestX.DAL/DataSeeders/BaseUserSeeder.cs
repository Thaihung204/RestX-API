using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Identity;
using Serilog;

namespace RestX.DAL.DataSeeders
{
    public abstract class BaseUserSeeder : IDataSeeder
    {
        protected readonly TenantDbContext Context;
        protected readonly PasswordHasher<ApplicationUser> PasswordHasher;
        protected BaseUserSeeder(TenantDbContext context)
        {
            Context = context;
            PasswordHasher = new PasswordHasher<ApplicationUser>();
        }
        public abstract int Order { get; }
        protected abstract string Email { get; }
        protected abstract string Username { get; }
        protected abstract string Password { get; }
        protected abstract string RoleName { get; }
        protected abstract string SeederName { get; }

        public async Task SeedAsync()
        {
            Log.Information($"[{SeederName}] Seeding user...");
            var normalizedEmail = Email.ToUpper();
            var exists = await Context.Set<ApplicationUser>()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail);
            if (exists)
            {
                Log.Information($"[{SeederName}] User already exists, skipping...");
                return;
            }
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, normalizedEmail);
            user.PasswordHash = PasswordHasher.HashPassword(user, Password);
            Context.Set<ApplicationUser>().Add(user);
            await Context.SaveChangesAsync();
            await AssignRoleAsync(userId);
            Log.Information($"[{SeederName}] User created: {Email}");
        }
        protected virtual ApplicationUser CreateUser(Guid userId, string normalizedEmail)
        {
            return new ApplicationUser
            {
                Id = userId,
                UserName = Username,
                NormalizedUserName = Username.ToUpper(),
                Email = Email,
                NormalizedEmail = normalizedEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                RefreshToken = string.Empty,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                LastModified = DateTime.UtcNow
            };
        }

        private async Task AssignRoleAsync(Guid userId)
        {
            var role = await Context.Set<IdentityRole<Guid>>()
                .FirstOrDefaultAsync(r => r.NormalizedName == RoleName.ToUpper());
            if (role == null) return;
            var userRole = new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = role.Id
            };
            Context.Set<IdentityUserRole<Guid>>().Add(userRole);
            await Context.SaveChangesAsync();
        }
    }
}

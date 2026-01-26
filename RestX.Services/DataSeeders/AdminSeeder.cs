using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Identity;
using Serilog;

namespace RestX.BLL.DataSeeders
{
    public class AdminSeeder
    {
        private readonly TenantDbContext context;
        private readonly string tenantHostname;
        private const string DEFAULT_ADMIN_PASSWORD = "Admin@123";
        private const string DEFAULT_ADMIN_USERNAME = "Admin";
        public AdminSeeder(TenantDbContext context, string tenantHostname)
        {
            this.context = context;
            this.tenantHostname = tenantHostname;
        }
        public async Task SeedAsync()
        {
            Log.Information("[AdminSeeder] Seeding admin user...");
            var adminEmail = $"admin@{tenantHostname}";
            var normalizedEmail = adminEmail.ToUpper();
            var existingAdmin = await context.Set<ApplicationUser>()
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail);
            if (existingAdmin)
            {
                Log.Information("[AdminSeeder] Admin user already exists, skipping...");
                return;
            }
            var adminUserId = Guid.NewGuid();
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = DEFAULT_ADMIN_USERNAME,
                NormalizedUserName = DEFAULT_ADMIN_USERNAME.ToUpper(),
                Email = adminEmail,
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

            var passwordHasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, DEFAULT_ADMIN_PASSWORD);
            context.Set<ApplicationUser>().Add(adminUser);
            await context.SaveChangesAsync();
            var adminRole = await context.Set<IdentityRole<Guid>>()
                .FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
            if (adminRole != null)
            {
                var userRole = new IdentityUserRole<Guid>
                {
                    UserId = adminUserId,
                    RoleId = adminRole.Id
                };
                context.Set<IdentityUserRole<Guid>>().Add(userRole);
                await context.SaveChangesAsync();
            }
            Log.Information($"[AdminSeeder] Admin account created: {adminEmail}");
        }
    }
}

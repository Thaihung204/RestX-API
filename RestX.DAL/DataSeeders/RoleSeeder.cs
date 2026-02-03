using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using Serilog;

namespace RestX.DAL.DataSeeders
{
    public class RoleSeeder : IDataSeeder
    {
        private readonly TenantDbContext context;
        private static readonly string[] DefaultRoles =
        {
            "System Admin",
            "Tenant Admin",
            "Kitchen Staff",
            "Waiter",
            "Customer"
        };
        public RoleSeeder(TenantDbContext context)
        {
            this.context = context;
        }
        public int Order => 2;
        public async Task SeedAsync()
        {
            Log.Information("[RoleSeeder] Seeding roles...");
            foreach (var roleName in DefaultRoles)
            {
                var exists = await context.Set<IdentityRole<Guid>>()
                    .AnyAsync(r => r.NormalizedName == roleName.ToUpper());
                if (exists) continue;
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                context.Set<IdentityRole<Guid>>().Add(role);
            }
            await context.SaveChangesAsync();
            Log.Information("[RoleSeeder] Roles seeded successfully");
        }
    }
}

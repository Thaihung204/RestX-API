using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using Serilog;

namespace RestX.BLL.DataSeeders
{
    public class RoleSeeder
    {
        private readonly TenantDbContext context;
        public RoleSeeder(TenantDbContext context)
        {
            this.context = context;
        }
        public async Task SeedAsync()
        {
            Log.Information("[RoleSeeder] Seeding roles...");
            var roles = new[] { "Admin", "Owner", "Kitchen Staff", "Waiter", "Customer" };
            foreach (var roleName in roles)
            {
                var roleExists = await context.Set<IdentityRole<Guid>>()
                    .AnyAsync(r => r.NormalizedName == roleName.ToUpper());
                if (!roleExists)
                {
                    var role = new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        NormalizedName = roleName.ToUpper(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };
                    context.Set<IdentityRole<Guid>>().Add(role);
                }
            }
            await context.SaveChangesAsync();
            Log.Information("[RoleSeeder] Roles seeded successfully");
        }
    }
}

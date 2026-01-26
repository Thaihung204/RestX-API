using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using RestX.Models.Identity;
using Serilog;

namespace RestX.BLL.DataSeeders
{
    public class TenantDataSeeder
    {
        private readonly string connectionString;
        private readonly string tenantHostname;
        public TenantDataSeeder(string connectionString, string tenantHostname)
        {
            this.connectionString = connectionString;
            this.tenantHostname = tenantHostname;
        }
        public async Task SeedAsync()
        {
            try
            {
                Log.Information($"[TenantDataSeeder] Starting seed Tier 1 data for tenant: {tenantHostname}");
                var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
                optionsBuilder.UseSqlServer(connectionString);
                using var context = new TenantDbContext(optionsBuilder.Options);
                await context.Database.MigrateAsync();
                Log.Information($"[TenantDataSeeder] Database migrated successfully");
                await SeedMinimalDataAsync(context);
                Log.Information($"[TenantDataSeeder] Seed data completed successfully for tenant: {tenantHostname}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[TenantDataSeeder] Failed to seed data for tenant: {tenantHostname}");
                throw;
            }
        }
        private async Task SeedMinimalDataAsync(TenantDbContext context)
        {
            Log.Information("[TenantDataSeeder] Seeding data ...");
            var statusSeeder = new StatusSystemSeeder(context);
            await statusSeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] StatusTypes & StatusValues seeded");
            var roleSeeder = new RoleSeeder(context);
            await roleSeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] Identity Roles seeded");
            var adminSeeder = new AdminSeeder(context, tenantHostname);
            await adminSeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] Admin User seeded");
            var loyaltySeeder = new LoyaltySeeder(context);
            await loyaltySeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] LoyaltyPointBands seeded");
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
            Log.Information("[TenantDataSeeder] Seeding Minimal data (Tier 1)...");

            var statusSeeder = new StatusSystemSeeder(context);
            await statusSeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] ✓ StatusTypes & StatusValues seeded");

           
            Log.Information("[TenantDataSeeder] ℹ Identity Roles & Admin User will be seeded by application on first run");

            var loyaltySeeder = new LoyaltySeeder(context);
            await loyaltySeeder.SeedAsync();
            Log.Information("[TenantDataSeeder] ✓ LoyaltyPointBands seeded");
        }

    }
}

using Microsoft.EntityFrameworkCore;
using RestX.DAL.Context;
using Serilog;

namespace RestX.DAL.DataSeeders
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
                Log.Information("[TenantDataSeeder] Starting seed data for tenant: {Hostname}", tenantHostname);
                await using var context = CreateDbContext();
                await MigrateDatabaseAsync(context);
                await RunSeedersAsync(context);

                Log.Information("[TenantDataSeeder] Seed data completed successfully for tenant: {Hostname}", tenantHostname);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TenantDataSeeder] Failed to seed data for tenant: {Hostname}", tenantHostname);
                throw;
            }
        }
        private TenantDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new TenantDbContext(optionsBuilder.Options);
        }
        private static async Task MigrateDatabaseAsync(TenantDbContext context)
        {
            await context.Database.MigrateAsync();
            Log.Information("[TenantDataSeeder] Database migrated successfully");
        }
        private async Task RunSeedersAsync(TenantDbContext context)
        {
            var seeders = CreateSeeders(context);
            foreach (var seeder in seeders.OrderBy(s => s.Order))
            {
                try
                {
                    await seeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[TenantDataSeeder] Seeder {Seeder} failed for tenant: {Hostname}", seeder.GetType().Name, tenantHostname);
                    throw;
                }
            }
        }
        private IEnumerable<IDataSeeder> CreateSeeders(TenantDbContext context)
        {
            return new IDataSeeder[]
            {
                new StatusSystemSeeder(context),   
                new RoleSeeder(context),           
                new SystemAdminSeeder(context),  
                new AdminSeeder(context, tenantHostname), 
                new LoyaltySeeder(context)   
            };
        }
    }
}

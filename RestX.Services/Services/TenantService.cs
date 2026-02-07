using AutoMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestX.AdminDAL.Context;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.DAL.DataSeeders;
using RestX.Models.Tenants;
using Serilog;
using System.Text.RegularExpressions;
using static Pipelines.Sockets.Unofficial.SocketConnection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RestX.BLL.Services
{
    public class TenantService : BaseService, ITenantService
    {
        private readonly RestxAdminContext adminContext;
        private readonly IRepository adminRepo;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;
        public TenantService(RestxAdminContext restxAdminContext, IRepository repo, IRedisService redisService, IMapper mapper, IConfiguration configuration, IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.adminContext = restxAdminContext;
            this.adminRepo = repo;
            this.mapper = mapper;
            this.configuration = configuration;
        }

        public async Task<IEnumerable<Tenant>> GetAllTenants()
        {
            var tenants = await adminRepo.GetAllAsync<Tenant>();
            return tenants.ToList();
        }

        public async Task<TenantOverview> GetTenantByIdOrHostname(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return null;

            Tenant? tenant = null;
            var cacheKey = $"tenant:{data.ToLower()}";

            var cachedTenant = await RedisService.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedTenant))
            {
                tenant = JsonConvert.DeserializeObject<Tenant>(cachedTenant);
            }

            if (tenant == null)
            {
                if (Guid.TryParse(data, out var tenantId))
                {
                    tenant = await adminRepo.GetByIdAsync<Tenant>(tenantId);
                }
                else
                {
                    tenant = await adminRepo.GetOneAsync<Tenant>(t => t.Hostname == data);
                }

                if (tenant != null)
                {
                    await RedisService.SetStringAsync(
                        cacheKey,
                        JsonConvert.SerializeObject(tenant)
                    );
                }
            }

            if (tenant == null)
                return null;

            return mapper.Map<TenantOverview>(tenant);
        }

        public async Task<Tenant> UpsertTenant(TenantItem model)
        {
            Tenant tenant;
            if (model.Id != null)
            {
                tenant = await adminRepo.GetByIdAsync<Tenant>(model.Id);
                tenant.Prefix = model.Prefix;
                tenant.Name = model.Name;
                tenant.LogoUrl = model.LogoUrl;
                tenant.FaviconUrl = model.FaviconUrl;
                tenant.BackgroundUrl = model.BackgroundUrl;
                tenant.BaseColor = model.BaseColor;
                tenant.PrimaryColor = model.PrimaryColor;
                tenant.SecondaryColor = model.SecondaryColor;
                tenant.NetworkIp = model.NetworkIp;
                tenant.ConnectionString = model.ConnectionString;
                tenant.Status = model.Status;
                tenant.Hostname = model.Hostname;
                tenant.ExpiredAt = model.ExpiredAt;

                adminRepo.Update(tenant);
                
                await adminRepo.SaveAsync();

                var oldIdCacheKey = $"tenant:{tenant.Id.ToString().ToLower()}";
                var oldHostnameCacheKey = $"tenant:{tenant.Hostname.ToLower()}";

                await RedisService.RemoveAsync(oldIdCacheKey);
                if (oldHostnameCacheKey != null)
                    await RedisService.RemoveAsync(oldHostnameCacheKey);
            }
            else
            {
                var databaseName = Regex.Replace(model.Name.Replace(" ", ""), "[^A-Za-z0-9 _]", "").ToLower();
                if (databaseName.Length > 15)
                {
                    databaseName = databaseName.Substring(0, 15);
                }

                var count = 1;
                var found = true;
                var name = databaseName;
                while (found)
                {
                    found = await this.adminRepo.GetExistsAsync<Tenant>(t => t.ConnectionString.Contains(name));
                    if (found)
                    {
                        count++;
                        name = $"{databaseName}{count}";
                    }
                }

                string tenantConnectionString = configuration["TenantConnectionStringTemplate"].Replace("{NAME}", name);

                tenant = new Tenant
                {
                    // Core
                    Name = model.Name,
                    Status = model.Status,
                    Hostname = model.Hostname,

                    // System / Identity
                    Prefix = string.Join("", model.Name.Split(" ", System.StringSplitOptions.RemoveEmptyEntries).Select(w => w.Substring(0, 1).ToUpper()).ToList()),
                    NetworkIp = model.NetworkIp ?? string.Empty,
                    ConnectionString = model.ConnectionString ?? tenantConnectionString,

                    // Theme / UI
                    BaseColor = model.BaseColor ?? "#FF380B",
                    PrimaryColor = model.PrimaryColor ?? "#6b7280",
                    SecondaryColor = model.SecondaryColor ?? "#9ca3af",
                    HeaderColor = model.HeaderColor ?? "#141927",
                    FooterColor = model.FooterColor ?? "#141927",

                    LogoUrl = model.LogoUrl ?? string.Empty,
                    FaviconUrl = model.FaviconUrl ?? string.Empty,
                    BackgroundUrl = model.BackgroundUrl ?? string.Empty,

                    // Expiry
                    ExpiredAt = model.ExpiredAt == default
                        ? DateTime.UtcNow.AddYears(1)
                        : model.ExpiredAt,

                    // Business
                    BusinessName = model.BusinessName,
                    BusinessAddressLine1 = model.BusinessAddressLine1,
                    BusinessAddressLine2 = model.BusinessAddressLine2,
                    BusinessAddressLine3 = model.BusinessAddressLine3,
                    BusinessAddressLine4 = model.BusinessAddressLine4,
                    BusinessCounty = model.BusinessCounty ?? string.Empty,
                    BusinessPostCode = model.BusinessPostCode ?? string.Empty,
                    BusinessCountry = model.BusinessCountry ?? string.Empty,
                    BusinessPrimaryPhone = model.BusinessPrimaryPhone,
                    BusinessSecondaryPhone = model.BusinessSecondaryPhone ?? string.Empty,
                    BusinessEmailAddress = model.BusinessEmailAddress,
                    BusinessCompanyNumber = model.BusinessCompanyNumber ?? string.Empty,
                    BusinessOpeningHours = model.BusinessOpeningHours ?? string.Empty,
                    AboutUs = model.AboutUs ?? string.Empty
                };

                await Repo.CreateAsync(tenant);
                await SeedTenantDataAsync(tenant);
            }
            return tenant;
        }


        private async Task SeedTenantDataAsync(Tenant tenant)
        {
            try
            {
                Log.Information($"[TenantService] Starting seed data for tenant: {tenant.Name} ({tenant.Hostname})");

                var seeder = new TenantDataSeeder(
                    tenant.ConnectionString,
                    tenant.Hostname
                );

                await seeder.SeedAsync();

                Log.Information($"[TenantService] ✓ Seed data completed for tenant: {tenant.Name}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[TenantService] ✗ Failed to seed data for tenant: {tenant.Name}");

               
                throw new Exception($"Failed to seed data for tenant {tenant.Name}. Tenant creation aborted.", ex);
            }
        }
        public async Task DeleteTenant(string id)
        {
            var tenant = await GetTenantByIdOrHostname(id);
            if (tenant != null)
            {
                adminRepo.Delete<Tenant>(id);
                await adminRepo.SaveAsync();
                var oldIdCacheKey = $"tenant:{id.ToString().ToLower()}";
                await RedisService.RemoveAsync(oldIdCacheKey);

            }
        }
    }
}

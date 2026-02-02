using AutoMapper;
using Newtonsoft.Json;
using RestX.AdminDAL.Context;
using RestX.BLL.DataSeeders;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.Extensions;
using RestX.BLL.Interfaces;
using RestX.Models.Tenants;
using Serilog;
using static Pipelines.Sockets.Unofficial.SocketConnection;

namespace RestX.BLL.Services
{
    public class TenantService : BaseService, ITenantService
    {
        private readonly RestxAdminContext adminContext;
        private readonly IRepository adminRepo;
        private readonly IMapper mapper;
        public TenantService(RestxAdminContext restxAdminContext, IRepository repo, IRedisService redisService, IMapper mapper, IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.adminContext = restxAdminContext;
            this.adminRepo = repo;
            this.mapper = mapper;
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

        public async Task<Tenant> UpsertTenant(Tenant model)
        {
            var tenant = new Tenant();
            if (model.Id != Guid.Empty)
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
            }
            else
            {
                await adminRepo.CreateAsync(model);
                tenant = model;

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
            }
        }
    }
}
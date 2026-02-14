using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
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
        private readonly ICloudinaryService cloudinaryService;
        public TenantService(ICloudinaryService cloudinaryService, RestxAdminContext restxAdminContext, IRepository repo, IRedisService redisService, IMapper mapper, IConfiguration configuration, IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.cloudinaryService = cloudinaryService;
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
            var cacheKey = $"Tenant:{data.ToLower()}";

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
                        $"Tenant:{tenant.Hostname}",
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
                var oldHostnameCacheKey = $"Tenant:{model.Hostname.ToLower()}";
                await RedisService.RemoveAsync(oldHostnameCacheKey);

                tenant = await adminRepo.GetByIdAsync<Tenant>(model.Id);

                if (model.LogoFile != null)
                {
                    await cloudinaryService.DeleteAsync($"{tenant.Name.Replace(" ", "")}/LogoUrl/logo");
                    tenant.LogoUrl = await HandleUploadTenantImage(model.LogoFile, $"{tenant.Name.Replace(" ", "")}/LogoUrl", "logo") ?? tenant.LogoUrl;
                }

                if (model.FaviconFile != null)
                {
                    await cloudinaryService.DeleteAsync($"{tenant.Name.Replace(" ", "")}/FaviconUrl/favicon");
                    tenant.FaviconUrl = await HandleUploadTenantImage(model.FaviconFile, $"{tenant.Name.Replace(" ", "")}/FaviconUrl", "favicon") ?? tenant.FaviconUrl;
                }

                if (model.BackgroundFile != null)
                {
                    await cloudinaryService.DeleteAsync($"{tenant.Name.Replace(" ", "")}/BackgroundUrl/background");
                    tenant.BackgroundUrl = await HandleUploadTenantImage(model.BackgroundFile, $"{tenant.Name.Replace(" ", "")}/BackgroundUrl", "background") ?? tenant.BackgroundUrl;
                }

                tenant.Prefix = model.Prefix
                    ?? string.Join("", model.Name.Split(" ", System.StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => w.Substring(0, 1).ToUpper()).ToList());

                tenant.Name = model.Name;

                tenant.PrimaryColor = model.PrimaryColor ?? "#FF380B";
                tenant.LightBaseColor = model.LightBaseColor ?? "#FFFFFF";
                tenant.LightSurfaceColor = model.LightSurfaceColor ?? "#F9FAFB";
                tenant.LightCardColor = model.LightCardColor ?? "#FFFFFF";
                tenant.DarkBaseColor = model.DarkBaseColor ?? "#0A0E14";
                tenant.DarkSurfaceColor = model.DarkSurfaceColor ?? "#1A1F2E";
                tenant.DarkCardColor = model.DarkCardColor ?? "#151A24";

                tenant.NetworkIp = model.NetworkIp ?? string.Empty;
                tenant.ConnectionString = model.ConnectionString ?? tenant.ConnectionString;

                tenant.Status = model.Status;
                tenant.Hostname = model.Hostname;

                tenant.ExpiredAt = model.ExpiredAt == default
                    ? DateTime.UtcNow.AddYears(1)
                    : model.ExpiredAt;

                tenant.BusinessName = model.BusinessName;
                tenant.BusinessAddressLine1 = model.BusinessAddressLine1;
                tenant.BusinessAddressLine2 = model.BusinessAddressLine2;
                tenant.BusinessAddressLine3 = model.BusinessAddressLine3;
                tenant.BusinessAddressLine4 = model.BusinessAddressLine4;
                tenant.BusinessCounty = model.BusinessCounty ?? string.Empty;
                tenant.BusinessPostCode = model.BusinessPostCode ?? string.Empty;
                tenant.BusinessCountry = model.BusinessCountry ?? string.Empty;
                tenant.BusinessPrimaryPhone = model.BusinessPrimaryPhone;
                tenant.BusinessSecondaryPhone = model.BusinessSecondaryPhone ?? string.Empty;
                tenant.BusinessEmailAddress = model.BusinessEmailAddress;
                tenant.BusinessCompanyNumber = model.BusinessCompanyNumber ?? string.Empty;
                tenant.BusinessOpeningHours = model.BusinessOpeningHours ?? string.Empty;
                tenant.AboutUs = model.AboutUs ?? string.Empty;

                adminRepo.Update(tenant);
                await adminRepo.SaveAsync();
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
                    PrimaryColor = model.PrimaryColor ?? "#FF380B",
                    LightBaseColor = model.LightBaseColor ?? "#FFFFFF",
                    LightSurfaceColor = model.LightSurfaceColor ?? "#F9FAFB",
                    LightCardColor = model.LightCardColor ?? "#FFFFFF",
                    DarkBaseColor = model.DarkBaseColor ?? "#0A0E14",
                    DarkSurfaceColor = model.DarkSurfaceColor ?? "#1A1F2E",
                    DarkCardColor = model.DarkCardColor ?? "#151A24",

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

                tenant.LogoUrl = await HandleUploadTenantImage(model.LogoFile, $"{tenant.Name.Replace(" ", "")}/LogoUrl", "logo") ?? tenant.LogoUrl;
                tenant.FaviconUrl = await HandleUploadTenantImage(model.FaviconFile, $"{tenant.Name.Replace(" ", "")}/FaviconUrl", "favicon") ?? tenant.FaviconUrl;
                tenant.BackgroundUrl = await HandleUploadTenantImage(model.BackgroundFile, $"{tenant.Name.Replace(" ", "")}/BackgroundUrl", "background") ?? tenant.BackgroundUrl;

                await Repo.CreateAsync(tenant);
                await SeedTenantDataAsync(tenant);
            }
            return tenant;
        }

        private async Task<string?> HandleUploadTenantImage(IFormFile? file, string folder, string publicId)
        {
            await using var stream = file.OpenReadStream();

            var upload = await cloudinaryService.UploadAsync(
                stream,
                file.FileName,
                folder,
                publicId: publicId,
                overwrite: true);

            return upload?.Url;
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
            var tenant = await adminRepo.GetByIdAsync<Tenant>(Guid.Parse(id));

            if (tenant == null)
                return;

            await DropTenantDatabaseAsync(tenant.ConnectionString);
            adminRepo.Delete<Tenant>(tenant.Id);
            await adminRepo.SaveAsync();
            await RedisService.RemoveAsync($"Tenant:{tenant.Hostname.ToLower()}");
        }

        private static async Task DropTenantDatabaseAsync(string tenantConnectionString)
        {
            if (string.IsNullOrWhiteSpace(tenantConnectionString))
                return;

            var tenantBuilder = new SqlConnectionStringBuilder(tenantConnectionString);
            var dbName = tenantBuilder.InitialCatalog;

            if (string.IsNullOrWhiteSpace(dbName))
                return;

            var masterBuilder = new SqlConnectionStringBuilder(tenantConnectionString)
            {
                InitialCatalog = "master"
            };

            const string sql = """
                               IF DB_ID(@dbName) IS NOT NULL
                               BEGIN
                                   DECLARE @sql NVARCHAR(MAX) =
                                       N'ALTER DATABASE ' + QUOTENAME(@dbName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
                                       N'DROP DATABASE ' + QUOTENAME(@dbName) + N';';
                                   EXEC sp_executesql @sql;
                               END
                               """;

            await using var conn = new SqlConnection(masterBuilder.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@dbName", System.Data.SqlDbType.NVarChar, 128) { Value = dbName });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<DataTranferObjects.Tenants.TenantRequest>> GetAllTenantRequests()
        {
            var entities = (await Repo.GetAllAsync<RestX.Models.Tenants.TenantRequest>(
                orderBy: q => q.OrderByDescending(r => r.CreatedDate)
            )).ToList();

            return mapper.Map<List<DataTranferObjects.Tenants.TenantRequest>>(entities);
        }

        public async Task<DataTranferObjects.Tenants.TenantRequest?> GetTenantRequestById(Guid tenantRequestsId)
        {
            var entity = await Repo.GetOneAsync<RestX.Models.Tenants.TenantRequest>(r => r.Id == tenantRequestsId);
            return mapper.Map<DataTranferObjects.Tenants.TenantRequest>(entity);
        }

        public async Task<Guid> AddTenantRequest(DataTranferObjects.Tenants.TenantRequest tenantRequest)
        {
            var entity = new RestX.Models.Tenants.TenantRequest
            {
                Name = tenantRequest.Name,
                Hostname = tenantRequest.Hostname,

                BusinessName = tenantRequest.BusinessName,
                BusinessPrimaryPhone = tenantRequest.BusinessPrimaryPhone,
                BusinessEmailAddress = tenantRequest.BusinessEmailAddress,

                BusinessAddressLine1 = tenantRequest.BusinessAddressLine1,
                BusinessAddressLine2 = tenantRequest.BusinessAddressLine2,
                BusinessAddressLine3 = tenantRequest.BusinessAddressLine3,
                BusinessAddressLine4 = tenantRequest.BusinessAddressLine4,
                BusinessCountry = tenantRequest.BusinessCountry,

                IsAccepted = tenantRequest.IsAccepted
            };

            await Repo.CreateAsync(entity);
            await Repo.SaveAsync();

            return entity.Id;
        }

        public async Task<Guid> ChangeStatus(Guid tenantRequestsId, bool? isAccepted)
        {
            var entity = await Repo.GetByIdAsync<RestX.Models.Tenants.TenantRequest>(tenantRequestsId);
            if (entity == null)
                return Guid.Empty;

            entity.IsAccepted = isAccepted;

            Repo.Update(entity);
            await Repo.SaveAsync();

            return entity.Id;
        }

        public async Task DeleteTenantRequest(Guid tenantRequestsId)
        {
            var entity = await Repo.GetByIdAsync<RestX.Models.Tenants.TenantRequest>(tenantRequestsId);
            if (entity == null)
                return;

            Repo.Delete<RestX.Models.Tenants.TenantRequest>(tenantRequestsId);
            await Repo.SaveAsync();
        }
    }
}

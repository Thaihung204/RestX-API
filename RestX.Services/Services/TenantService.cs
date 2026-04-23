using AutoMapper;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestX.AdminDAL.Context;
using RestX.BLL.DataTranferObjects.Tenants;
using RestX.BLL.Exceptionhandling;
using RestX.BLL.Interfaces;
using RestX.DAL.DataSeeders;
using RestX.Models.Enum;
using RestX.Models.Tenants;
using Serilog;
using System.Text.RegularExpressions;
using TenantConfiguration = RestX.BLL.DataTranferObjects.Tenants.TenantConfiguration;

namespace RestX.BLL.Services
{
    public class TenantService : BaseService, ITenantService
    {
        private readonly RestxAdminContext adminContext;
        private readonly IRepository adminRepo;
        private readonly IMapper mapper;
        private readonly IConfiguration configuration;
        private readonly ICloudinaryService cloudinaryService;
        private readonly ILogger<TenantService> logger;
        private readonly IEmailService emailService;

        public TenantService(
            ILogger<TenantService> logger,
            ICloudinaryService cloudinaryService,
            RestxAdminContext restxAdminContext,
            IRepository repo,
            IRedisService redisService,
            IMapper mapper,
            IConfiguration configuration,
            IEmailService emailService,
            IEnumerable<ActiveTenant> tenant = null) : base(repo, redisService, tenant)
        {
            this.logger = logger;
            this.cloudinaryService = cloudinaryService;
            this.adminContext = restxAdminContext;
            this.adminRepo = repo;
            this.mapper = mapper;
            this.configuration = configuration;
            this.emailService = emailService;
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
            var cacheKey = $"{data.ToLower():Tenant}";

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
                    tenant = await adminRepo.GetOneAsync<Tenant>(t => t.Hostname == data && t.Status);
                }

                if (tenant != null)
                {
                    await RedisService.SetStringAsync(
                        $"{tenant.Hostname}:Tenant",
                        JsonConvert.SerializeObject(tenant)
                    );
                }
            }

            if (tenant == null)
                return null;

            return mapper.Map<TenantOverview>(tenant);
        }

        public async Task<Tenant> UpdateTenant(TenantItem model)
        {
            logger.LogInformation("===== UpsertTenant START =====");
            logger.LogInformation("Model Id: {Id}, Name: {Name}, Hostname: {Hostname}",
                model.Id, model.Name, model.Hostname);

            Tenant tenant;

            if (model.Id != null)
            {
                logger.LogInformation("FLOW: UPDATE");

                try
                {
                    var oldHostnameCacheKey = $"{model.Hostname?.ToLower():Tenant}";
                    logger.LogInformation("Removing Redis key: {Key}", oldHostnameCacheKey);

                    await RedisService.RemoveAsync(oldHostnameCacheKey);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Redis remove failed");
                    throw;
                }

                tenant = await adminRepo.GetByIdAsync<Tenant>(model.Id);

                if (tenant == null)
                {
                    logger.LogError("Tenant NOT FOUND with Id: {Id}", model.Id);
                    throw new Exception("Tenant not found");
                }

                logger.LogInformation("Tenant loaded successfully: {TenantName}", tenant.Name);

                // ===== LOGO =====
                if (model.LogoFile != null)
                {
                    logger.LogInformation("Logo file detected. Size: {Size}", model.LogoFile.Length);

                    var path = $"{tenant.Name.Replace(" ", "")}/LogoUrl/logo";
                    logger.LogInformation("Deleting old logo: {Path}", path);

                    await cloudinaryService.DeleteAsync(path);

                    logger.LogInformation("Uploading new logo...");

                    tenant.LogoUrl = await HandleUploadTenantImage(
                        model.LogoFile,
                        $"{tenant.Name.Replace(" ", "")}/LogoUrl",
                        "logo") ?? tenant.LogoUrl;

                    logger.LogInformation("Logo updated: {LogoUrl}", tenant.LogoUrl);
                }

                // ===== FAVICON =====
                if (model.FaviconFile != null)
                {
                    logger.LogInformation("Favicon file detected. Size: {Size}", model.FaviconFile.Length);

                    var path = $"{tenant.Name.Replace(" ", "")}/FaviconUrl/favicon";
                    logger.LogInformation("Deleting old favicon: {Path}", path);

                    await cloudinaryService.DeleteAsync(path);

                    logger.LogInformation("Uploading new favicon...");

                    tenant.FaviconUrl = await HandleUploadTenantImage(
                        model.FaviconFile,
                        $"{tenant.Name.Replace(" ", "")}/FaviconUrl",
                        "favicon") ?? tenant.FaviconUrl;

                    logger.LogInformation("Favicon updated: {FaviconUrl}", tenant.FaviconUrl);
                }

                // ===== BACKGROUND =====
                if (model.BackgroundFile != null)
                {
                    logger.LogInformation("Background file detected. Size: {Size}", model.BackgroundFile.Length);

                    var path = $"{tenant.Name.Replace(" ", "")}/BackgroundUrl/background";
                    logger.LogInformation("Deleting old background: {Path}", path);

                    await cloudinaryService.DeleteAsync(path);

                    logger.LogInformation("Uploading new background...");

                    tenant.BackgroundUrl = await HandleUploadTenantImage(
                        model.BackgroundFile,
                        $"{tenant.Name.Replace(" ", "")}/BackgroundUrl",
                        "background") ?? tenant.BackgroundUrl;

                    logger.LogInformation("Background updated: {BackgroundUrl}", tenant.BackgroundUrl);
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
                    ? DateTime.UtcNow.AddHours(7).AddYears(1)
                    : model.ExpiredAt;
                tenant.TaxRate = model.TaxRate;
                tenant.ServiceChargeRate = model.ServiceChargeRate;

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

                TenantConfiguration properties = JsonConvert.DeserializeObject<TenantConfiguration>(JsonConvert.SerializeObject(model.Configuration)); // action.CustomProperties.ToObject<CreateEnquiryHistoryProperties>();
                tenant.PropertiesJson = JsonConvert.SerializeObject(properties);

                adminRepo.Update(tenant);
                await adminRepo.SaveAsync();
            }
            else
            {
                logger.LogInformation("FLOW: CREATE — delegating to CreateTenantAsync");
                await CreateTenant(model);
                tenant = await adminRepo.GetOneAsync<Tenant>(t => t.Hostname == model.Hostname);
            }

            return tenant;
        }

        public async Task<string> UploadAndCreateTenant(TenantItem model)
        {
            string folderPrefix = model.Name?.Replace(" ", "") ?? "unknown";

            if (model.LogoFile != null)
            {
                model.LogoUrl = await HandleUploadTenantImage(
                    model.LogoFile, $"{folderPrefix}/LogoUrl", "logo") ?? model.LogoUrl;
                model.LogoFile = null;
            }

            if (model.FaviconFile != null)
            {
                model.FaviconUrl = await HandleUploadTenantImage(
                    model.FaviconFile, $"{folderPrefix}/FaviconUrl", "favicon") ?? model.FaviconUrl;
                model.FaviconFile = null;
            }

            if (model.BackgroundFile != null)
            {
                model.BackgroundUrl = await HandleUploadTenantImage(
                    model.BackgroundFile, $"{folderPrefix}/BackgroundUrl", "background") ?? model.BackgroundUrl;
                model.BackgroundFile = null;
            }

            string jobId = BackgroundJob.Enqueue<ITenantService>(s => s.CreateTenant(model));

            logger.LogInformation("Tenant creation job enqueued for: {Name} | JobId: {JobId}", model.Name, jobId);
            return jobId;
        }
        public async Task CreateTenant(TenantItem model)
        {
            logger.LogInformation("===== CreateTenantAsync START | Name: {Name} =====", model.Name);

            var databaseName = Regex.Replace(RemoveVietnameseDiacritics(model.Name).Replace(" ", ""), "[^A-Za-z0-9 _]", "").ToLower();
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

            TenantConfiguration properties = JsonConvert.DeserializeObject<TenantConfiguration>(JsonConvert.SerializeObject(model.Configuration)); // action.CustomProperties.ToObject<CreateEnquiryHistoryProperties>();
            
            var tenant = new Tenant
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
                    ? DateTime.UtcNow.AddHours(7).AddYears(1)
                    : model.ExpiredAt,

                TaxRate = model.TaxRate,
                ServiceChargeRate = model.ServiceChargeRate,

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
                AboutUs = model.AboutUs ?? string.Empty,

                //Property Json
                PropertiesJson = JsonConvert.SerializeObject(properties)
            };

            await using IDbContextTransaction transaction = await adminContext.Database.BeginTransactionAsync();

            try
            {
                await Repo.CreateAsync(tenant);
                await transaction.CommitAsync();

                logger.LogInformation("Tenant record created. Seeding data for: {TenantName}", tenant.Name);
                await SeedTenantDataAsync(tenant);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed to create tenant record for: {Name}", model.Name);
                throw;
            }

        }
        private async Task<string?> HandleUploadTenantImage(IFormFile? file, string folder, string publicId)
        {
            logger.LogInformation("---- HandleUploadTenantImage START ----");

            if (file == null)
            {
                logger.LogWarning("Upload skipped: file is NULL | Folder: {Folder} | PublicId: {PublicId}",
                    folder, publicId);
                return null;
            }

            try
            {
                logger.LogInformation(
                    "Upload Info | FileName: {FileName} | Size: {Size} | ContentType: {ContentType} | Folder: {Folder} | PublicId: {PublicId}",
                    file.FileName,
                    file.Length,
                    file.ContentType,
                    folder,
                    publicId);

                await using var stream = file.OpenReadStream();

                logger.LogInformation("Stream opened successfully.");

                var upload = await cloudinaryService.UploadAsync(
                    stream,
                    file.FileName,
                    folder,
                    publicId: publicId,
                    overwrite: true);

                if (upload == null)
                {
                    logger.LogError("Cloudinary Upload returned NULL | Folder: {Folder} | PublicId: {PublicId}",
                        folder, publicId);
                    return null;
                }

                logger.LogInformation("Cloudinary Upload SUCCESS | Url: {Url}", upload.Url);

                logger.LogInformation("---- HandleUploadTenantImage END SUCCESS ----");

                return upload.Url;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Cloudinary Upload FAILED | FileName: {FileName} | Folder: {Folder} | PublicId: {PublicId}",
                    file?.FileName,
                    folder,
                    publicId);

                throw;
            }
        }

        private static string RemoveVietnameseDiacritics(string text)
        {
            var vietnameseChars = new Dictionary<char, char>
            {
                {'á', 'a'}, {'à', 'a'}, {'ả', 'a'}, {'ã', 'a'}, {'ạ', 'a'},
                {'ă', 'a'}, {'ắ', 'a'}, {'ằ', 'a'}, {'ẳ', 'a'}, {'ẵ', 'a'}, {'ặ', 'a'},
                {'â', 'a'}, {'ấ', 'a'}, {'ầ', 'a'}, {'ẩ', 'a'}, {'ẫ', 'a'}, {'ậ', 'a'},
                {'Á', 'A'}, {'À', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Ạ', 'A'},
                {'Ă', 'A'}, {'Ắ', 'A'}, {'Ằ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'}, {'Ặ', 'A'},
                {'Â', 'A'}, {'Ấ', 'A'}, {'Ầ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ậ', 'A'},

                {'é', 'e'}, {'è', 'e'}, {'ẻ', 'e'}, {'ẽ', 'e'}, {'ẹ', 'e'},
                {'ê', 'e'}, {'ế', 'e'}, {'ề', 'e'}, {'ể', 'e'}, {'ễ', 'e'}, {'ệ', 'e'},
                {'É', 'E'}, {'È', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ẹ', 'E'},
                {'Ê', 'E'}, {'Ế', 'E'}, {'Ề', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'}, {'Ệ', 'E'},

                {'í', 'i'}, {'ì', 'i'}, {'ỉ', 'i'}, {'ĩ', 'i'}, {'ị', 'i'},
                {'Í', 'I'}, {'Ì', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'}, {'Ị', 'I'},

                {'ó', 'o'}, {'ò', 'o'}, {'ỏ', 'o'}, {'õ', 'o'}, {'ọ', 'o'},
                {'ô', 'o'}, {'ố', 'o'}, {'ồ', 'o'}, {'ổ', 'o'}, {'ỗ', 'o'}, {'ộ', 'o'},
                {'ơ', 'o'}, {'ớ', 'o'}, {'ờ', 'o'}, {'ở', 'o'}, {'ỡ', 'o'}, {'ợ', 'o'},
                {'Ó', 'O'}, {'Ò', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ọ', 'O'},
                {'Ô', 'O'}, {'Ố', 'O'}, {'Ồ', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ộ', 'O'},
                {'Ơ', 'O'}, {'Ớ', 'O'}, {'Ờ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'}, {'Ợ', 'O'},

                {'ú', 'u'}, {'ù', 'u'}, {'ủ', 'u'}, {'ũ', 'u'}, {'ụ', 'u'},
                {'ư', 'u'}, {'ứ', 'u'}, {'ừ', 'u'}, {'ử', 'u'}, {'ữ', 'u'}, {'ự', 'u'},
                {'Ú', 'U'}, {'Ù', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ụ', 'U'},
                {'Ư', 'U'}, {'Ứ', 'U'}, {'Ừ', 'U'}, {'Ử', 'U'}, {'Ữ', 'U'}, {'Ự', 'U'},

                {'ý', 'y'}, {'ỳ', 'y'}, {'ỷ', 'y'}, {'ỹ', 'y'}, {'ỵ', 'y'},
                {'Ý', 'Y'}, {'Ỳ', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'}, {'Ỵ', 'Y'},

                {'đ', 'd'}, {'Đ', 'D'}
            };

            var result = new char[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                result[i] = vietnameseChars.TryGetValue(text[i], out var replacement)
                    ? replacement
                    : text[i];
            }
            return new string(result);
        }

        public async Task SeedTenantDataAsync(Tenant tenant)
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
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new AppException("Tenant id is required.");
            }

            if (!Guid.TryParse(id, out Guid tenantId))
            {
                throw new AppException("Tenant id is invalid.");
            }

            try
            {
                Tenant? tenant = await adminRepo.GetByIdAsync<Tenant>(tenantId);

                if (tenant == null)
                {
                    throw new AppException("Tenant not found.");
                }

                await DropTenantDatabaseAsync(tenant.ConnectionString);

                adminRepo.Delete<Tenant>(tenant.Id);
                await adminRepo.SaveAsync();

                await RedisService.RemoveAsync($"{tenant.Hostname.ToLower()}:Tenant");
                await RedisService.RemoveAsync($"{tenant.Hostname}:Tenant");
            }
            catch (AppException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                logger.LogError(ex, "DeleteTenant SQL error. TenantId: {TenantId}", tenantId);
                throw new AppException($"Database error while deleting tenant: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                logger.LogError(ex, "DeleteTenant timeout. TenantId: {TenantId}", tenantId);
                throw new AppException("Delete tenant timed out. Please try again.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "DeleteTenant invalid operation. TenantId: {TenantId}", tenantId);
                throw new AppException($"Cannot delete tenant due to invalid operation: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DeleteTenant unexpected error. TenantId: {TenantId}", tenantId);
                throw new AppException($"Unexpected error while deleting tenant: {ex.Message}");
            }
        }

        public async Task ChangeTenantStatus(Guid id, bool status)
        {
            if (id == Guid.Empty)
            {
                throw new AppException("Tenant id is required.");
            }

            Tenant? tenant = await adminRepo.GetByIdAsync<Tenant>(id);
            if (tenant == null)
            {
                throw new AppException("Tenant not found.");
            }

            if (tenant.Status == status)
            {
                return;
            }

            tenant.Status = status;
            adminRepo.Update(tenant);
            await adminRepo.SaveAsync();

            await RedisService.RemoveAsync($"{tenant.Hostname}:Tenant");
            await RedisService.RemoveAsync($"{tenant.Hostname.ToLower()}:Tenant");
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
            var entity = new Models.Tenants.TenantRequest
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
            };

            await Repo.CreateAsync(entity);

            var submittedAt = DateTime.UtcNow.AddHours(7).ToString("dd/MM/yyyy HH:mm");
            try
            {
                await emailService.SendTenantRequestSubmittedAsync(
                    entity.BusinessEmailAddress,
                    entity.BusinessName ?? entity.Name,
                    submittedAt);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send submitted email for tenant request: {Name}", entity.Name);
            }

            return entity.Id;
        }

        public async Task<string> AcceptTenantRequest(Guid tenantRequestsId)
        {
            Models.Tenants.TenantRequest tenantRequest = await Repo.GetByIdAsync<Models.Tenants.TenantRequest>(tenantRequestsId);
            if (tenantRequest == null)
                return string.Empty;

            tenantRequest.tenantRequestStatus = TenantRequestStatus.Accepted;
            Repo.Update(tenantRequest);
            await Repo.SaveAsync();

            TenantItem tenantItem = mapper.Map<TenantItem>(tenantRequest);
            tenantItem.Id = null;

            string jobId = await UploadAndCreateTenant(tenantItem);

            try
            {
                await emailService.SendTenantRequestAcceptedAsync(
                    tenantRequest.BusinessEmailAddress,
                    tenantRequest.BusinessName ?? tenantRequest.Name,
                    tenantRequest.Hostname);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send accepted email for tenant request: {Name}", tenantRequest.Name);
            }

            return jobId;
        }
        public async Task<Guid> DeclineTenantRequest(Guid tenantRequestsId)
        {
            var request = await Repo.GetByIdAsync<Models.Tenants.TenantRequest>(tenantRequestsId);
            if (request == null)
                return Guid.Empty;

            request.tenantRequestStatus = TenantRequestStatus.Denied;

            Repo.Update(request);
            await Repo.SaveAsync();

            try
            {
                await emailService.SendTenantRequestRejectedAsync(
                    request.BusinessEmailAddress,
                    request.BusinessName ?? request.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send rejected email for tenant request: {Name}", request.Name);
            }

            return request.Id;
        }

        public async Task DeleteTenantRequest(Guid tenantRequestsId)
        {
            var entity = await Repo.GetByIdAsync<RestX.Models.Tenants.TenantRequest>(tenantRequestsId);
            if (entity == null)
                return;

            Repo.Delete<RestX.Models.Tenants.TenantRequest>(tenantRequestsId);
            await Repo.SaveAsync();
        }

        public async Task<IEnumerable<DataTranferObjects.Tenants.BusinessHourDto>> GetBusinessHours(Guid tenantId)
        {
            var hours = await adminContext.TenantBusinessHours
                .Where(bh => bh.TenantId == tenantId)
                .OrderBy(bh => bh.DayOfWeek)
                .ToListAsync();

            return hours.Select(bh => new DataTranferObjects.Tenants.BusinessHourDto
            {
                DayOfWeek = bh.DayOfWeek,
                OpenTime = bh.OpenTime,
                CloseTime = bh.CloseTime,
                IsClosed = bh.IsClosed
            });
        }

        public async Task UpdateBusinessHours(Guid tenantId, IEnumerable<DataTranferObjects.Tenants.BusinessHourDto> hours)
        {
            var tenant = await adminContext.Tenants.FindAsync(tenantId);
            if (tenant == null)
                throw new InvalidOperationException("Tenant not found");

            var existing = adminContext.TenantBusinessHours.Where(bh => bh.TenantId == tenantId);
            adminContext.TenantBusinessHours.RemoveRange(existing);

            var entities = hours.Select(h => new RestX.Models.Tenants.TenantBusinessHour
            {
                TenantId = tenantId,
                DayOfWeek = h.DayOfWeek,
                OpenTime = h.OpenTime,
                CloseTime = h.CloseTime,
                IsClosed = h.IsClosed
            });
            await adminContext.TenantBusinessHours.AddRangeAsync(entities);
            await adminContext.SaveChangesAsync();
        }
    }
}

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.Models.Tenants;
using System.Net;

public class CloudinaryService : BaseService, ICloudinaryService
{
    private readonly Cloudinary cloudinary;
    private readonly ILogger<TenantService> logger;

    public CloudinaryService(
        ILogger<TenantService> logger,
        IConfiguration configuration,
        IRepository repo,
        IRedisService redisService,
        IEnumerable<ActiveTenant> tenant = null
    ) : base(repo, redisService, tenant)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]
        );
        this.logger = logger;
        cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadAsync(
    Stream fileStream,
    string fileName,
    string folder,
    string? publicId = null,
    bool overwrite = false)
    {
        logger.LogInformation("==== Cloudinary UploadAsync START ====");
        logger.LogInformation(
            "Params | FileName: {FileName} | Folder: {Folder} | PublicId: {PublicId} | Overwrite: {Overwrite}",
            fileName,
            folder,
            publicId,
            overwrite);

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = $"{folder}".Trim('/'),
                PublicId = publicId,
                Overwrite = overwrite,
                UseFilename = publicId == null,
                UniqueFilename = publicId == null
            };

            logger.LogInformation("Calling cloudinary.UploadAsync...");

            var result = await cloudinary.UploadAsync(uploadParams);

            logger.LogInformation(
                "Cloudinary Raw Response | StatusCode: {StatusCode} | PublicId: {ResultPublicId} | SecureUrl: {SecureUrl} | Error: {Error}",
                result.StatusCode,
                result.PublicId,
                result.SecureUrl,
                result.Error?.Message);

            // 🚨 Check lỗi từ Cloudinary
            if (result.Error != null)
            {
                logger.LogError("Cloudinary returned error: {ErrorMessage}", result.Error.Message);
                throw new Exception($"Cloudinary error: {result.Error.Message}");
            }

            if (result.SecureUrl == null)
            {
                logger.LogError("Cloudinary SecureUrl is NULL");
                throw new Exception("Cloudinary upload failed - SecureUrl is null");
            }

            logger.LogInformation("==== Cloudinary UploadAsync SUCCESS ====");

            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Cloudinary UploadAsync FAILED | FileName: {FileName} | Folder: {Folder}",
                fileName,
                folder);

            throw;
        }
    }

    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        await cloudinary.DestroyAsync(new DeletionParams($"{publicId}"));
    }
}

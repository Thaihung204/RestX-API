using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using RestX.BLL.Interfaces;
using RestX.BLL.Services;
using RestX.Models.Tenants;
using System.Net;

public class CloudinaryService : BaseService, ICloudinaryService
{
    private readonly Cloudinary cloudinary;

    public CloudinaryService(
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

        cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string folder,
        string? publicId = null,
        bool overwrite = false)
    {
        
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = $"{CurrentTenant.Name.Replace(" ", "")}/{folder}".Trim('/'),
            PublicId = publicId,
            Overwrite = overwrite,
            UseFilename = publicId == null,
            UniqueFilename = publicId == null
        };

        var result = await cloudinary.UploadAsync(uploadParams);

        return new CloudinaryUploadResult
        {
            Url = result.SecureUrl.ToString(),
            PublicId = result.PublicId
        };
    }

    public async Task DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        await cloudinary.DestroyAsync(new DeletionParams($"{CurrentTenant.Name.Replace(" ", "")}/{publicId}"));
    }
    public async Task DeleteFolderImageByPrefix(string prefix)
    {
        await cloudinary.DeleteResourcesByPrefixAsync($"{CurrentTenant.Name.Replace(" ", "")}/{prefix}");
    }
}

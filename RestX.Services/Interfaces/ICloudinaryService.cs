public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string folder,
        string? publicId = null,
        bool overwrite = false
    );

    Task DeleteAsync(string publicId);
    Task DeleteFolderImageByPrefix(string prefix);
}

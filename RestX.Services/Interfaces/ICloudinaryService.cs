public interface ICloudinaryService
{
    Task<string> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder
    );

    Task DeleteImageAsync(string publicId);
}

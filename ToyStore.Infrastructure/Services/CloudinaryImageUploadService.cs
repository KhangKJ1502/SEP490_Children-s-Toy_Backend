using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Application.Interfaces.Services;

namespace ToyStore.Infrastructure.Services;

public class CloudinaryImageUploadService : IImageUploadService
{
    private const string DefaultUploadFolder = "SEP490_Products";
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryImageUploadService> _logger;

    public CloudinaryImageUploadService(IConfiguration configuration, ILogger<CloudinaryImageUploadService> logger)
    {
        _logger = logger;
        
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("Cloudinary configuration is missing or incomplete.");
        }

        var account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<Result<string>> UploadImageAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        return await UploadCoreAsync(fileStream, fileName, DefaultUploadFolder, cancellationToken);
    }

    public async Task<Result<string>> UploadImageToFolderAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = DefaultUploadFolder;
        }

        return await UploadCoreAsync(fileStream, fileName, folder, cancellationToken);
    }

    private async Task<Result<string>> UploadCoreAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            return Result<string>.Failure("VALIDATION_ERROR", "No file was provided.");
        }

        // Check file extension
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Result<string>.Failure("VALIDATION_ERROR", "Invalid file type. Allowed types: jpg, jpeg, png, gif, webp.");
        }

        // Check file size (e.g. max 5MB)
        if (fileStream.Length > 5 * 1024 * 1024)
        {
            return Result<string>.Failure("VALIDATION_ERROR", "File size exceeds the 5MB limit.");
        }

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Message}", uploadResult.Error.Message);
                return Result<string>.Failure("UPLOAD_ERROR", "Failed to upload image to Cloudinary.");
            }

            return Result<string>.Success(uploadResult.SecureUrl.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary.");
            return Result<string>.Failure("UPLOAD_ERROR", "An unexpected error occurred during image upload.");
        }
    }
}

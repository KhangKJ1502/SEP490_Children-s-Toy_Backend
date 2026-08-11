using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces.Services;

/// <summary>
/// Service for uploading images to external storage (e.g. Cloudinary).
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Uploads an image file and returns its URL.
    /// </summary>
    Task<Result<string>> UploadImageAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image file to the provided folder and returns its URL.
    /// </summary>
    Task<Result<string>> UploadImageToFolderAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image directly from a source URL to Cloudinary (no local download required).
    /// </summary>
    Task<Result<string>> UploadImageFromUrlAsync(
        string sourceUrl,
        string folder,
        string? publicId = null,
        CancellationToken cancellationToken = default);
}


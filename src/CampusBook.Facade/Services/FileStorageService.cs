using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CampusBook.Facade.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string[] _permittedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeInBytes = 2 * 1024 * 1024; // 2MB limit

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public bool IsValidImage(IFormFile file, out string? errorMessage)
    {
        if (file.Length == 0)
        {
            errorMessage = "The uploaded file is empty.";
            return false;
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            errorMessage = "File size exceeds the 2MB limit.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_permittedExtensions.Contains(extension))
        {
            errorMessage = "Invalid image type. Permitted formats: JPG, JPEG, PNG, WEBP.";
            return false;
        }

        var contentType = file.ContentType.ToLowerInvariant();
        if (!contentType.StartsWith("image/"))
        {
            errorMessage = "Uploaded file content is not a recognized image.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
    {
        if (!IsValidImage(file, out var error))
        {
            throw new ArgumentException(error ?? "Invalid image file");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", subFolder);

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsDir, safeFileName);

        await using (var fileStream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        _logger.LogInformation("Saved uploaded file to {PhysicalPath}", physicalPath);

        // Return relative path for web serving
        return $"/uploads/{subFolder}/{safeFileName}";
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("/uploads/"))
        {
            return;
        }

        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(webRoot, normalized);

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Deleted file at {PhysicalPath}", physicalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete file at {RelativePath}", relativePath);
        }
    }
}


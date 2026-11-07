using Microsoft.AspNetCore.Http;

namespace CampusBook.Facade.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder);
    void DeleteFile(string? relativePath);
    bool IsValidImage(IFormFile file, out string? errorMessage);
}


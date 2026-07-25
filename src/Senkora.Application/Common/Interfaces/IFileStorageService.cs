namespace Senkora.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct = default);
    Task DeleteAsync(string storedPath, CancellationToken ct = default);
    string GetPublicUrl(string storedPath);
}

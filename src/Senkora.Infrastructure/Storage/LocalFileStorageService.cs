using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Senkora.Application.Common.Interfaces;

namespace Senkora.Infrastructure.Storage;

/// <summary>
/// Yerel dosya sistemi depolamasi.
/// Uretimde Azure Blob / S3 ile degistirilebilir (ayni interface).
/// </summary>
public sealed class LocalFileStorageService(
    IConfiguration config,
    ILogger<LocalFileStorageService> logger) : IFileStorageService
{
    private readonly string _root = Path.IsPathRooted(config["FileStorage:LocalPath"] ?? "")
        ? config["FileStorage:LocalPath"]!
        : Path.Combine(Directory.GetCurrentDirectory(),
            config["FileStorage:LocalPath"] ?? "uploads");

    private readonly string _baseUrl =
        config["FileStorage:BaseUrl"] ?? "http://localhost:5000/uploads";

    public async Task<string> SaveAsync(
        string fileName, Stream content, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        logger.LogInformation("Dosya kaydedildi: {Path}", fullPath);
        return fileName.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storedPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Dosya bulunamadi: {storedPath}", fullPath);

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedPath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storedPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storedPath)
        => $"{_baseUrl.TrimEnd('/')}/{storedPath.Replace('\\', '/')}";
}

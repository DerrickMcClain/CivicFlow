using CivicFlow.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CivicFlow.Infrastructure.Storage;

public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private string RootPath => configuration["BlobStorage:LocalRoot"] ?? "./data/documents";

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task<StoredFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storageKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<StoredFileContent?>(null);
        }

        Stream stream = File.OpenRead(fullPath);
        var contentType = GetContentType(Path.GetExtension(fullPath));
        return Task.FromResult<StoredFileContent?>(new StoredFileContent
        {
            Content = stream,
            ContentType = contentType
        });
    }

    private string GetFullPath(string storageKey)
    {
        var normalizedKey = storageKey.Replace('\\', '/').TrimStart('/');
        var combined = Path.GetFullPath(Path.Combine(RootPath, normalizedKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootFull = Path.GetFullPath(RootPath);
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }

        return combined;
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".txt" => "text/plain",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };
}

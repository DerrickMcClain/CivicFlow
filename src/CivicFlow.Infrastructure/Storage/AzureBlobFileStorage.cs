using Azure.Storage.Blobs;
using CivicFlow.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CivicFlow.Infrastructure.Storage;

public sealed class AzureBlobFileStorage(IConfiguration configuration) : IFileStorage
{
    private BlobContainerClient Container => new(
        configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is required."),
        configuration["BlobStorage:ContainerName"] ?? "civicflow-documents");

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        await Container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = Container.GetBlobClient(storageKey);
        await blob.UploadAsync(content, overwrite: true, cancellationToken);
    }

    public async Task<StoredFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = Container.GetBlobClient(storageKey);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new StoredFileContent
        {
            Content = download.Value.Content,
            ContentType = download.Value.Details.ContentType ?? "application/octet-stream"
        };
    }
}

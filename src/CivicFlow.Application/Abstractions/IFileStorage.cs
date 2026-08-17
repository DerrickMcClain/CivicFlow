namespace CivicFlow.Application.Abstractions;

public sealed class StoredFileContent
{
    public required Stream Content { get; init; }
    public required string ContentType { get; init; }
}

public interface IFileStorage
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);

    Task<StoredFileContent?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}

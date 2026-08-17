namespace CivicFlow.Domain.Entities;

public class RequestDocument
{
    public int DocumentId { get; set; }
    public int RequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsInternal { get; set; }
    public ServiceRequest Request { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}

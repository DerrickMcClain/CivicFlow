namespace CivicFlow.Application.Documents;

public static class DocumentConstants
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;
    public const int MaxFilesPerRequest = 10;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "text/plain",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png",
        ".txt",
        ".doc",
        ".docx",
    };
}

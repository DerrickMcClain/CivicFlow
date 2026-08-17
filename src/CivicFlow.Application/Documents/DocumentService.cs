using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Documents;

public sealed class DocumentService(IAppDbContext db, IFileStorage storage, IAuditLogger audit)
{
    public async Task<DocumentDto> UploadAsync(
        int requestId,
        int userId,
        RoleName role,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        bool isInternal,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        ValidateUpload(fileName, contentType, sizeBytes);

        var request = await db.ServiceRequests
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request not found.");

        EnsureCanAccessRequest(request, userId, role);
        EnsureMutable(request.Status.StatusName);

        if (role == RoleName.Citizen)
        {
            if (isInternal)
            {
                throw new ForbiddenException("Citizens cannot upload internal documents.");
            }
        }
        else if (role is not (RoleName.Employee or RoleName.Supervisor or RoleName.Administrator))
        {
            throw new ForbiddenException("You cannot upload documents for this request.");
        }

        var existingCount = await db.RequestDocuments
            .CountAsync(x => x.RequestId == requestId, cancellationToken);
        if (existingCount >= DocumentConstants.MaxFilesPerRequest)
        {
            throw new ValidationException($"A request can have at most {DocumentConstants.MaxFilesPerRequest} documents.");
        }

        var document = new RequestDocument
        {
            RequestId = requestId,
            FileName = Path.GetFileName(fileName.Trim()),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            IsInternal = isInternal,
            StorageKey = string.Empty
        };

        db.RequestDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        document.StorageKey = BuildStorageKey(requestId, document.DocumentId, document.FileName);
        await storage.SaveAsync(document.StorageKey, content, cancellationToken);
        request.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(
            userId,
            "DOCUMENT_UPLOADED",
            "ServiceRequest",
            request.RequestNumber,
            null,
            document.FileName,
            ip,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var uploader = await db.Users
            .AsNoTracking()
            .SingleAsync(x => x.UserId == userId, cancellationToken);

        return Map(document, uploader);
    }

    public async Task<DocumentDownloadDto> DownloadAsync(
        int requestId,
        int documentId,
        int userId,
        RoleName role,
        CancellationToken cancellationToken = default)
    {
        var document = await db.RequestDocuments
            .Include(x => x.Request)
            .FirstOrDefaultAsync(
                x => x.DocumentId == documentId && x.RequestId == requestId,
                cancellationToken)
            ?? throw new NotFoundException("Document not found.");

        EnsureCanAccessRequest(document.Request, userId, role);
        if (role == RoleName.Citizen && document.IsInternal)
        {
            throw new ForbiddenException("You do not have access to this document.");
        }

        var stored = await storage.OpenReadAsync(document.StorageKey, cancellationToken)
            ?? throw new NotFoundException("Document content was not found.");

        return new DocumentDownloadDto
        {
            Content = stored.Content,
            FileName = document.FileName,
            ContentType = stored.ContentType
        };
    }

    internal static IReadOnlyList<DocumentDto> MapForDetail(
        ServiceRequest request,
        bool includeInternalDocuments)
    {
        return request.Documents
            .Where(x => includeInternalDocuments || !x.IsInternal)
            .OrderBy(x => x.UploadedAt)
            .Select(x => Map(x, x.UploadedByUser))
            .ToList();
    }

    private static DocumentDto Map(RequestDocument document, User uploadedBy) => new()
    {
        DocumentId = document.DocumentId,
        FileName = document.FileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        UploadedByName = $"{uploadedBy.FirstName} {uploadedBy.LastName}".Trim(),
        UploadedAt = document.UploadedAt,
        IsInternal = document.IsInternal
    };

    private static void ValidateUpload(string fileName, string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ValidationException("A file name is required.");
        }

        if (sizeBytes <= 0)
        {
            throw new ValidationException("The uploaded file is empty.");
        }

        if (sizeBytes > DocumentConstants.MaxFileSizeBytes)
        {
            throw new ValidationException("The uploaded file exceeds the 10 MB limit.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !DocumentConstants.AllowedExtensions.Contains(extension))
        {
            throw new ValidationException("That file type is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(contentType)
            || !DocumentConstants.AllowedContentTypes.Contains(contentType))
        {
            throw new ValidationException("That file type is not allowed.");
        }
    }

    private static void EnsureCanAccessRequest(ServiceRequest request, int userId, RoleName role)
    {
        if (role == RoleName.Citizen && request.CitizenId != userId)
        {
            throw new ForbiddenException("You do not have access to this service request.");
        }
    }

    private static void EnsureMutable(RequestStatusName status)
    {
        if (WorkflowPolicy.IsTerminal(status))
        {
            throw new ConflictException("Closed requests cannot be modified.");
        }
    }

    private static string BuildStorageKey(int requestId, int documentId, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return $"{requestId}/{documentId}/{Guid.NewGuid():N}-{safeName}";
    }
}

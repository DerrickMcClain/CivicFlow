using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Requests;

public sealed class RequestService(
    IAppDbContext db,
    IRequestNumberGenerator numbers,
    IAuditLogger audit)
{
    public async Task<ServiceRequestDetailDto> CreateAsync(
        int citizenId,
        CreateRequestRequest dto,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new ValidationException("Title and description are required.");
        }

        var requestType = await db.ServiceRequestTypes
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.ServiceRequestTypeId == dto.RequestTypeId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Service request type not found.");

        var submitted = await db.RequestStatuses
            .SingleAsync(x => x.StatusId == (int)RequestStatusName.Submitted, cancellationToken);
        var now = DateTime.UtcNow;
        var requestNumber = await numbers.NextAsync(cancellationToken);

        var request = new ServiceRequest
        {
            RequestNumber = requestNumber,
            CitizenId = citizenId,
            RequestTypeId = requestType.ServiceRequestTypeId,
            StatusId = submitted.StatusId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Priority = dto.Priority == 0 ? Priority.Medium : dto.Priority,
            CreatedAt = now,
            UpdatedAt = now,
            SubmittedAt = now
        };
        request.StatusHistory.Add(new RequestStatusHistory
        {
            OldStatusId = null,
            NewStatusId = submitted.StatusId,
            ChangedByUserId = citizenId,
            Reason = "Case submitted",
            ChangedAt = now
        });

        db.ServiceRequests.Add(request);
        await audit.WriteAsync(
            citizenId,
            "CASE_CREATED",
            "ServiceRequest",
            requestNumber,
            null,
            requestNumber,
            ip,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(request.RequestId, citizenId, RoleName.Citizen, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestListDto>> ListMineAsync(
        int citizenId,
        CancellationToken cancellationToken = default)
    {
        return await db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.CitizenId == citizenId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ServiceRequestListDto
            {
                RequestId = x.RequestId,
                RequestNumber = x.RequestNumber,
                Title = x.Title,
                Status = x.Status.StatusName.ToString(),
                Priority = x.Priority,
                CreatedAt = x.CreatedAt,
                SubmittedAt = x.SubmittedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequestDetailDto> GetAsync(
        int requestId,
        int userId,
        RoleName role,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadDetailAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("Service request not found.");

        if (role == RoleName.Citizen && request.CitizenId != userId)
        {
            throw new ForbiddenException("You do not have access to this service request.");
        }

        return MapDetail(request, includeInternalNotes: role != RoleName.Citizen);
    }

    public async Task<ServiceRequestDetailDto> RespondAsync(
        int requestId,
        int citizenId,
        string message,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ValidationException("A response message is required.");
        }

        var request = await db.ServiceRequests
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken)
            ?? throw new NotFoundException("Service request not found.");

        if (request.CitizenId != citizenId)
        {
            throw new ForbiddenException("You do not have access to this service request.");
        }

        var from = request.Status.StatusName;
        if (WorkflowPolicy.IsTerminal(from))
        {
            throw new ConflictException("Closed requests cannot be modified.");
        }

        if (!WorkflowPolicy.CanTransition(from, RequestStatusName.UnderReview, RoleName.Citizen, isOwner: true))
        {
            throw new ConflictException("That status transition is not allowed.");
        }

        var underReview = await db.RequestStatuses
            .SingleAsync(x => x.StatusId == (int)RequestStatusName.UnderReview, cancellationToken);
        var now = DateTime.UtcNow;

        request.Notes.Add(new CaseNote
        {
            AuthorId = citizenId,
            NoteText = message.Trim(),
            CreatedAt = now,
            IsInternal = false
        });
        request.StatusHistory.Add(new RequestStatusHistory
        {
            OldStatusId = request.StatusId,
            NewStatusId = underReview.StatusId,
            ChangedByUserId = citizenId,
            Reason = "Citizen provided additional information",
            ChangedAt = now
        });
        request.StatusId = underReview.StatusId;
        request.UpdatedAt = now;

        await audit.WriteAsync(
            citizenId,
            "CASE_INFO_RESPONDED",
            "ServiceRequest",
            request.RequestNumber,
            from.ToString(),
            RequestStatusName.UnderReview.ToString(),
            ip,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(request.RequestId, citizenId, RoleName.Citizen, cancellationToken);
    }

    private async Task<ServiceRequest?> LoadDetailAsync(int requestId, CancellationToken cancellationToken)
    {
        return await db.ServiceRequests
            .Include(x => x.Status)
            .Include(x => x.RequestType)
                .ThenInclude(x => x.Department)
            .Include(x => x.AssignedEmployee)
            .Include(x => x.Notes)
                .ThenInclude(x => x.Author)
            .Include(x => x.StatusHistory)
                .ThenInclude(x => x.OldStatus)
            .Include(x => x.StatusHistory)
                .ThenInclude(x => x.NewStatus)
            .Include(x => x.StatusHistory)
                .ThenInclude(x => x.ChangedByUser)
            .FirstOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
    }

    private static ServiceRequestDetailDto MapDetail(ServiceRequest request, bool includeInternalNotes)
    {
        var notes = request.Notes
            .Where(x => includeInternalNotes || !x.IsInternal)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new NoteDto
            {
                NoteId = x.NoteId,
                NoteText = x.NoteText,
                AuthorName = $"{x.Author.FirstName} {x.Author.LastName}".Trim(),
                CreatedAt = x.CreatedAt,
                IsInternal = x.IsInternal
            })
            .ToList();

        var history = request.StatusHistory
            .OrderBy(x => x.ChangedAt)
            .Select(x => new StatusHistoryDto
            {
                OldStatus = x.OldStatus?.StatusName.ToString(),
                NewStatus = x.NewStatus.StatusName.ToString(),
                Reason = x.Reason,
                ChangedByName = $"{x.ChangedByUser.FirstName} {x.ChangedByUser.LastName}".Trim(),
                ChangedAt = x.ChangedAt
            })
            .ToList();

        return new ServiceRequestDetailDto
        {
            RequestId = request.RequestId,
            RequestNumber = request.RequestNumber,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status.StatusName.ToString(),
            Priority = request.Priority,
            RequestTypeName = request.RequestType.Name,
            DepartmentName = request.RequestType.Department.DepartmentName,
            AssignedEmployeeName = request.AssignedEmployee is null
                ? null
                : $"{request.AssignedEmployee.FirstName} {request.AssignedEmployee.LastName}".Trim(),
            CreatedAt = request.CreatedAt,
            SubmittedAt = request.SubmittedAt,
            CompletedAt = request.CompletedAt,
            Notes = notes,
            History = history
        };
    }
}

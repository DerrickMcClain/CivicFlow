using CivicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Role> Roles { get; }
    DbSet<Department> Departments { get; }
    DbSet<User> Users { get; }
    DbSet<RequestStatus> RequestStatuses { get; }
    DbSet<ServiceRequestType> ServiceRequestTypes { get; }
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistories { get; }
    DbSet<CaseNote> CaseNotes { get; }
    DbSet<AssignmentHistory> AssignmentHistories { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

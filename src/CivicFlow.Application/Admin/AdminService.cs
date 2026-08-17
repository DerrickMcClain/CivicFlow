using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Admin;

public sealed class AdminService(IAppDbContext db)
{
    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await db.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Department)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new AdminUserDto
            {
                UserId = x.UserId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                Role = x.Role.RoleName.ToString(),
                DepartmentId = x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.DepartmentName : null,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(
        int userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(x => x.Role)
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.Role.RoleName == RoleName.Administrator && request.Role != RoleName.Administrator)
        {
            var adminCount = await db.Users.CountAsync(
                x => x.Role.RoleName == RoleName.Administrator && x.IsActive,
                cancellationToken);
            if (adminCount <= 1)
            {
                throw new ConflictException("The system must keep at least one administrator.");
            }
        }

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await db.Departments.AnyAsync(
                x => x.DepartmentId == request.DepartmentId.Value,
                cancellationToken);
            if (!departmentExists)
            {
                throw new NotFoundException("Department not found.");
            }
        }

        user.RoleId = (int)request.Role;
        user.DepartmentId = request.DepartmentId;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        user.Role = await db.Roles.SingleAsync(x => x.RoleId == user.RoleId, cancellationToken);
        user.Department = user.DepartmentId is null
            ? null
            : await db.Departments.SingleAsync(x => x.DepartmentId == user.DepartmentId, cancellationToken);

        return ToUserDto(user);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(
        UpsertDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            throw new ValidationException("Department name is required.");
        }

        var department = new Department
        {
            DepartmentName = request.DepartmentName.Trim(),
            Description = request.Description?.Trim()
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return ToDepartmentDto(department);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(
        int departmentId,
        UpsertDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            throw new ValidationException("Department name is required.");
        }

        var department = await db.Departments.FirstOrDefaultAsync(
            x => x.DepartmentId == departmentId,
            cancellationToken)
            ?? throw new NotFoundException("Department not found.");

        department.DepartmentName = request.DepartmentName.Trim();
        department.Description = request.Description?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return ToDepartmentDto(department);
    }

    public async Task<RequestTypeDto> CreateRequestTypeAsync(
        UpsertRequestTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Request type name is required.");
        }

        var departmentExists = await db.Departments.AnyAsync(
            x => x.DepartmentId == request.DepartmentId,
            cancellationToken);
        if (!departmentExists)
        {
            throw new NotFoundException("Department not found.");
        }

        var type = new ServiceRequestType
        {
            DepartmentId = request.DepartmentId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        db.ServiceRequestTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);
        return ToRequestTypeDto(type);
    }

    public async Task<RequestTypeDto> UpdateRequestTypeAsync(
        int requestTypeId,
        UpsertRequestTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Request type name is required.");
        }

        var type = await db.ServiceRequestTypes.FirstOrDefaultAsync(
            x => x.ServiceRequestTypeId == requestTypeId,
            cancellationToken)
            ?? throw new NotFoundException("Request type not found.");

        var departmentExists = await db.Departments.AnyAsync(
            x => x.DepartmentId == request.DepartmentId,
            cancellationToken);
        if (!departmentExists)
        {
            throw new NotFoundException("Department not found.");
        }

        type.DepartmentId = request.DepartmentId;
        type.Name = request.Name.Trim();
        type.Description = request.Description?.Trim();
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return ToRequestTypeDto(type);
    }

    public async Task<IReadOnlyList<AuditLogDto>> ListAuditLogsAsync(
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);

        return await db.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .OrderByDescending(x => x.Timestamp)
            .Take(take)
            .Select(x => new AuditLogDto
            {
                AuditLogId = x.AuditLogId,
                UserId = x.UserId,
                UserEmail = x.User != null ? x.User.Email : null,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                OldValues = x.OldValues,
                NewValues = x.NewValues,
                IpAddress = x.IpAddress,
                Timestamp = x.Timestamp
            })
            .ToListAsync(cancellationToken);
    }

    private static AdminUserDto ToUserDto(User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role.RoleName.ToString(),
        DepartmentId = user.DepartmentId,
        DepartmentName = user.Department?.DepartmentName,
        IsActive = user.IsActive
    };

    private static DepartmentDto ToDepartmentDto(Department department) => new()
    {
        DepartmentId = department.DepartmentId,
        DepartmentName = department.DepartmentName,
        Description = department.Description
    };

    private static RequestTypeDto ToRequestTypeDto(ServiceRequestType type) => new()
    {
        ServiceRequestTypeId = type.ServiceRequestTypeId,
        DepartmentId = type.DepartmentId,
        Name = type.Name,
        Description = type.Description,
        IsActive = type.IsActive
    };
}

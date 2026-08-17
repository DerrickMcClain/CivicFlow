using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Infrastructure.Seed;

public sealed class DbSeeder(CivicFlowDbContext db, IPasswordHasher<User> passwordHasher)
{
    public const string DemoPassword = "CivicFlow!dev1";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedStatusesAsync(cancellationToken);
        var department = await SeedDepartmentAsync(cancellationToken);
        await SeedRequestTypeAsync(department.DepartmentId, cancellationToken);
        await SeedUsersAsync(department.DepartmentId, cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in Enum.GetValues<RoleName>())
        {
            var roleId = (int)roleName;
            if (await db.Roles.AnyAsync(x => x.RoleId == roleId, cancellationToken))
            {
                continue;
            }

            db.Roles.Add(new Role { RoleId = roleId, RoleName = roleName });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedStatusesAsync(CancellationToken cancellationToken)
    {
        foreach (var statusName in Enum.GetValues<RequestStatusName>())
        {
            var statusId = (int)statusName;
            if (await db.RequestStatuses.AnyAsync(x => x.StatusId == statusId, cancellationToken))
            {
                continue;
            }

            db.RequestStatuses.Add(new RequestStatus
            {
                StatusId = statusId,
                StatusName = statusName,
                IsTerminal = statusName is RequestStatusName.Completed or RequestStatusName.Cancelled
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Department> SeedDepartmentAsync(CancellationToken cancellationToken)
    {
        var department = await db.Departments
            .FirstOrDefaultAsync(x => x.DepartmentName == "Planning & Development", cancellationToken);

        if (department is not null)
        {
            return department;
        }

        department = new Department
        {
            DepartmentName = "Planning & Development",
            Description = "Permits, land use, and related citizen service requests."
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return department;
    }

    private async Task SeedRequestTypeAsync(int departmentId, CancellationToken cancellationToken)
    {
        if (await db.ServiceRequestTypes.AnyAsync(x => x.Name == "Residential Permit", cancellationToken))
        {
            return;
        }

        db.ServiceRequestTypes.Add(new ServiceRequestType
        {
            DepartmentId = departmentId,
            Name = "Residential Permit",
            Description = "Residential building and improvement permits.",
            IsActive = true
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(int departmentId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await EnsureUserAsync(
            "Casey", "Citizen", "citizen@civicflow.local", RoleName.Citizen, departmentId: null, now, cancellationToken);
        await EnsureUserAsync(
            "Ellis", "Employee", "employee@civicflow.local", RoleName.Employee, departmentId, now, cancellationToken);
        await EnsureUserAsync(
            "Sage", "Supervisor", "supervisor@civicflow.local", RoleName.Supervisor, departmentId, now, cancellationToken);
        await EnsureUserAsync(
            "Avery", "Admin", "admin@civicflow.local", RoleName.Administrator, departmentId: null, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(
        string firstName,
        string lastName,
        string email,
        RoleName role,
        int? departmentId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return;
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            RoleId = (int)role,
            DepartmentId = departmentId,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
        db.Users.Add(user);
    }
}

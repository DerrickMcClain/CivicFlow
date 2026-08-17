using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Domain.Workflow;
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
        await SeedPolicyArticlesAsync(cancellationToken);
        await BackfillSlaDueDatesAsync(cancellationToken);
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

    private async Task SeedPolicyArticlesAsync(CancellationToken cancellationToken)
    {
        if (await db.PolicyArticles.AnyAsync(cancellationToken))
        {
            return;
        }

        db.PolicyArticles.AddRange(
            new PolicyArticle
            {
                Title = "Residential deck permits",
                Summary = "When a building permit is required for deck work.",
                Body = "Deck additions and replacements typically require a residential building permit when the deck is attached to the home or exceeds 30 inches above grade. Submit plans showing dimensions, setbacks, and railing details.",
                Keywords = "deck, permit, residential, railing, setback"
            },
            new PolicyArticle
            {
                Title = "Fence height and location",
                Summary = "Standard fence rules for residential lots.",
                Body = "Front-yard fences are generally limited to 4 feet. Side and rear fences may be up to 6 feet. Corner lots may have additional sight-line restrictions.",
                Keywords = "fence, height, residential, corner lot"
            },
            new PolicyArticle
            {
                Title = "Required documents for permit review",
                Summary = "Common attachments staff expect during review.",
                Body = "Include a site plan, elevation drawings, and photos of the existing conditions when available. Additional information may be requested while a case is under review.",
                Keywords = "documents, site plan, drawings, review"
            });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task BackfillSlaDueDatesAsync(CancellationToken cancellationToken)
    {
        var requests = await db.ServiceRequests
            .Where(x => x.SlaDueAt == null && x.SubmittedAt != null)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            request.SlaDueAt = SlaPolicy.ComputeDueAt(request.Priority, request.SubmittedAt!.Value);
        }

        if (requests.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

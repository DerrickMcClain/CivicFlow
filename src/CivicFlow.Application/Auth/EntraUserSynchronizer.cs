using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Auth;

public sealed class EntraUserSynchronizer(IAppDbContext db)
{
    public async Task<User> SyncAsync(EntraIdentity identity, CancellationToken cancellationToken = default)
    {
        var objectId = identity.ObjectId.Trim();
        var email = identity.Email.Trim();
        var firstName = identity.FirstName.Trim();
        var lastName = identity.LastName.Trim();
        if (string.IsNullOrEmpty(firstName))
        {
            firstName = "Entra";
        }

        if (string.IsNullOrEmpty(lastName))
        {
            lastName = "User";
        }

        var user = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.EntraObjectId == objectId, cancellationToken);

        if (user is not null)
        {
            if (!user.IsActive)
            {
                throw new ForbiddenException("This account is inactive.");
            }

            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.RoleId = (int)identity.Role;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            user.Role = await db.Roles.SingleAsync(x => x.RoleId == user.RoleId, cancellationToken);
            return user;
        }

        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var now = DateTime.UtcNow;
        user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = EntraAuthConstants.PasswordSentinel,
            EntraObjectId = objectId,
            RoleId = (int)identity.Role,
            DepartmentId = null,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        user.Role = await db.Roles.SingleAsync(x => x.RoleId == user.RoleId, cancellationToken);
        return user;
    }
}

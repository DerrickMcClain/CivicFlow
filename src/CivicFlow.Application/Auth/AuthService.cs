using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CivicFlow.Application.Auth;

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await db.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!string.IsNullOrEmpty(user.EntraObjectId))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return ToResponse(user);
    }

    public async Task<AuthResponse> RegisterCitizenAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName)
            || string.IsNullOrWhiteSpace(request.LastName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("First name, last name, email, and password are required.");
        }

        var email = request.Email.Trim();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with that email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            RoleId = (int)RoleName.Citizen,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        user.Role = await db.Roles.SingleAsync(x => x.RoleId == (int)RoleName.Citizen, cancellationToken);
        return ToResponse(user);
    }

    private AuthResponse ToResponse(User user) => new()
    {
        Token = jwtTokenService.CreateToken(user),
        UserId = user.UserId,
        Email = user.Email,
        Role = user.Role.RoleName.ToString(),
        FirstName = user.FirstName,
        LastName = user.LastName
    };
}

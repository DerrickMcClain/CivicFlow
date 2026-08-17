using CivicFlow.Application.Auth;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class EntraUserSynchronizerTests
{
    private readonly CivicFlowApiFactory _factory;

    public EntraUserSynchronizerTests(SqlServerFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Sync_new_oid_creates_user_with_citizen_role()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var sut = new EntraUserSynchronizer(db);

        var oid = Guid.NewGuid().ToString();
        var email = $"jit-{oid}@example.test";

        var user = await sut.SyncAsync(new EntraIdentity(
            oid, email, "Jamie", "Newcomer", RoleName.Citizen));

        Assert.True(user.UserId > 0);
        Assert.Equal(oid, user.EntraObjectId);
        Assert.Equal(email, user.Email);
        Assert.Equal("Jamie", user.FirstName);
        Assert.Equal("Newcomer", user.LastName);
        Assert.Equal(EntraAuthConstants.PasswordSentinel, user.PasswordHash);
        Assert.True(user.IsActive);
        Assert.Null(user.DepartmentId);
        Assert.Equal(RoleName.Citizen, user.Role.RoleName);
    }

    [Fact]
    public async Task Sync_same_oid_does_not_duplicate()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var sut = new EntraUserSynchronizer(db);

        var oid = Guid.NewGuid().ToString();
        var email = $"dup-{oid}@example.test";
        var identity = new EntraIdentity(oid, email, "Pat", "Repeat", RoleName.Citizen);

        var first = await sut.SyncAsync(identity);
        var second = await sut.SyncAsync(identity);

        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(1, await db.Users.CountAsync(x => x.EntraObjectId == oid));
    }

    [Fact]
    public async Task Sync_updates_role_from_identity()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var sut = new EntraUserSynchronizer(db);

        var oid = Guid.NewGuid().ToString();
        var email = $"role-{oid}@example.test";

        await sut.SyncAsync(new EntraIdentity(oid, email, "Riley", "Staff", RoleName.Citizen));
        var updated = await sut.SyncAsync(new EntraIdentity(
            oid, $"promoted-{oid}@example.test", "Riley", "Employee", RoleName.Employee));

        Assert.Equal(RoleName.Employee, updated.Role.RoleName);
        Assert.Equal($"promoted-{oid}@example.test", updated.Email);
        Assert.Equal("Employee", updated.LastName);
    }

    [Fact]
    public async Task Sync_inactive_throws_403()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var oid = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            FirstName = "Inactive",
            LastName = "Person",
            Email = $"inactive-{oid}@example.test",
            PasswordHash = EntraAuthConstants.PasswordSentinel,
            EntraObjectId = oid,
            RoleId = (int)RoleName.Citizen,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = false
        });
        await db.SaveChangesAsync();

        var sut = new EntraUserSynchronizer(db);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.SyncAsync(new EntraIdentity(
                oid, $"inactive-{oid}@example.test", "Inactive", "Person", RoleName.Citizen)));

        Assert.Equal("This account is inactive.", ex.Message);
        Assert.Equal(403, ex.Status);
    }

    [Fact]
    public async Task Sync_email_collision_with_seed_throws_409()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var sut = new EntraUserSynchronizer(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.SyncAsync(new EntraIdentity(
                Guid.NewGuid().ToString(),
                "citizen@civicflow.local",
                "Casey",
                "Citizen",
                RoleName.Citizen)));

        Assert.Equal("An account with this email already exists.", ex.Message);
        Assert.Equal(409, ex.Status);
    }
}

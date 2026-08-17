using CivicFlow.Domain.Enums;

namespace CivicFlow.Application.Auth;

public sealed record EntraIdentity(
    string ObjectId,
    string Email,
    string FirstName,
    string LastName,
    RoleName Role);

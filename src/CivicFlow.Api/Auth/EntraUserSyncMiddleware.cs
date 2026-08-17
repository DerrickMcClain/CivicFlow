using System.Security.Claims;
using System.Text.Json;
using CivicFlow.Application.Auth;
using CivicFlow.Application.Common;
using CivicFlow.Domain.Enums;

namespace CivicFlow.Api.Auth;

/// <summary>
/// Turns an authenticated Entra principal into the CivicFlow principal the rest of the app expects:
/// a JIT user row plus <see cref="ClaimTypes.NameIdentifier"/> = SQL user id and a CivicFlow role.
/// </summary>
public sealed class EntraUserSyncMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public const string MissingRoleMessage =
        "An Entra app role of Citizen, Employee, Supervisor, or Administrator is required.";
    public const string MissingObjectIdMessage =
        "The Entra token is missing the object id (oid) claim.";

    private const string ObjectIdClaimType =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context, EntraUserSynchronizer synchronizer)
    {
        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true || !IsEntraPrincipal(principal))
        {
            await next(context);
            return;
        }

        if (!TryGetRole(principal, out var role))
        {
            await WriteEnvelopeAsync(context, StatusCodes.Status403Forbidden, MissingRoleMessage);
            return;
        }

        var objectId = FirstValue(principal, "oid", ObjectIdClaimType);
        if (string.IsNullOrWhiteSpace(objectId))
        {
            await WriteEnvelopeAsync(context, StatusCodes.Status403Forbidden, MissingObjectIdMessage);
            return;
        }

        var email = FirstValue(principal, "email", "preferred_username", ClaimTypes.Email) ?? string.Empty;
        var (firstName, lastName) = ReadName(principal);

        Domain.Entities.User user;
        try
        {
            user = await synchronizer.SyncAsync(
                new EntraIdentity(objectId, email, firstName, lastName, role),
                context.RequestAborted);
        }
        catch (AppException ex)
        {
            await WriteEnvelopeAsync(context, ex.Status, ex.Message);
            return;
        }

        var roleName = user.Role.RoleName.ToString();
        var identity = new ClaimsIdentity(
            CivicFlowAuthSchemes.Entra,
            ClaimTypes.NameIdentifier,
            ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        identity.AddClaim(new Claim("role", roleName));
        identity.AddClaim(new Claim("email", user.Email));
        context.User = new ClaimsPrincipal(identity);

        await next(context);
    }

    private bool IsEntraPrincipal(ClaimsPrincipal principal)
    {
        var issuer = principal.FindFirst("iss")?.Value;
        if (issuer is null)
        {
            return false;
        }

        return !string.Equals(issuer, configuration["Jwt:Issuer"], StringComparison.Ordinal);
    }

    private static bool TryGetRole(ClaimsPrincipal principal, out RoleName role)
    {
        var roles = principal.Claims
            .Where(x => x.Type is "roles" or "role" || x.Type == ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct(StringComparer.Ordinal)
            .Select(value => Enum.TryParse<RoleName>(value, out var parsed) ? parsed : (RoleName?)null)
            .Where(x => x is not null)
            .Distinct()
            .ToList();

        if (roles.Count != 1)
        {
            role = default;
            return false;
        }

        role = roles[0]!.Value;
        return true;
    }

    private static (string FirstName, string LastName) ReadName(ClaimsPrincipal principal)
    {
        var firstName = FirstValue(principal, "given_name", ClaimTypes.GivenName);
        var lastName = FirstValue(principal, "family_name", ClaimTypes.Surname);
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
        {
            return (firstName ?? string.Empty, lastName ?? string.Empty);
        }

        var name = FirstValue(principal, "name", ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return (string.Empty, string.Empty);
        }

        var separator = name.IndexOf(' ');
        return separator < 0
            ? (name, string.Empty)
            : (name[..separator], name[(separator + 1)..].Trim());
    }

    private static string? FirstValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static async Task WriteEnvelopeAsync(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status,
            message,
            traceId = context.TraceIdentifier
        }, JsonOptions));
    }
}

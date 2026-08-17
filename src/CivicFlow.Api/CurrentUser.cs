using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CivicFlow.Domain.Enums;

namespace CivicFlow.Api;

public static class CurrentUser
{
    public static int GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The user id claim is missing.");
        return int.Parse(value);
    }

    public static RoleName GetRole(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.Role)
            ?? user.FindFirstValue("role")
            ?? throw new InvalidOperationException("The role claim is missing.");
        return Enum.Parse<RoleName>(value);
    }
}

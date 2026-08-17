using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CivicFlow.Tests;

/// <summary>
/// Mints stand-in Entra access tokens with the symmetric key the test host trusts.
/// Nothing here contacts login.microsoftonline.com.
/// </summary>
public static class EntraTokenHelper
{
    public const string TenantId = "test";
    public const string ClientId = "civicflow-test-api";
    public const string Audience = "api://civicflow-test";
    public const string Issuer = "https://login.microsoftonline.com/test/v2.0";
    public const string SigningKey = "TEST_ENTRA_SIGNING_KEY_32CHARS_MIN!!";

    public static string CreateAccessToken(
        string? role,
        string oid,
        string email,
        string first = "Ada",
        string last = "Tester")
    {
        var claims = new List<Claim>
        {
            new("oid", oid),
            new("sub", oid),
            new("email", email),
            new("preferred_username", email),
            new("given_name", first),
            new("family_name", last),
            new("name", $"{first} {last}")
        };

        if (role is not null)
        {
            claims.Add(new Claim("roles", role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

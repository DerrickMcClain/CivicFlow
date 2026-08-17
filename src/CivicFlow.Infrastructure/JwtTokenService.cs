using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicFlow.Application.Abstractions;
using CivicFlow.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CivicFlow.Infrastructure;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateToken(User user)
    {
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var minutes)
            ? minutes
            : 480;

        var role = user.Role.RoleName.ToString();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("email", user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

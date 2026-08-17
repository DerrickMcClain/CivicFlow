using System.Buffers.Text;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CivicFlow.Application.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace CivicFlow.Api.Auth;

public static class CivicFlowAuthSchemes
{
    public const string Local = "CivicFlow";
    public const string Entra = "Entra";
    public const string Selector = "CivicFlowSelector";
}

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCivicFlowAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var localIssuer = jwt["Issuer"];
        var localSigningKey = jwt["SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var azureAd = configuration.GetSection("AzureAd");
        var entraEnabled = !string.IsNullOrWhiteSpace(azureAd["TenantId"])
            && !string.IsNullOrWhiteSpace(azureAd["ClientId"]);

        services
            .AddAuthentication(CivicFlowAuthSchemes.Selector)
            .AddPolicyScheme(CivicFlowAuthSchemes.Selector, CivicFlowAuthSchemes.Selector, options =>
            {
                options.ForwardDefaultSelector = context => SelectScheme(
                    context.Request.Headers.Authorization,
                    localIssuer,
                    entraEnabled);
            })
            .AddJwtBearer(CivicFlowAuthSchemes.Local, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = localIssuer,
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localSigningKey)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        if (entraEnabled)
        {
            services.AddAuthentication().AddJwtBearer(CivicFlowAuthSchemes.Entra, options =>
            {
                // Raw claim names keep oid / roles / preferred_username intact for the sync middleware.
                options.MapInboundClaims = false;
                var testSigningKey = azureAd["SigningKey"];
                if (string.IsNullOrWhiteSpace(testSigningKey))
                {
                    var instance = azureAd["Instance"] ?? "https://login.microsoftonline.com/";
                    options.Authority = $"{instance.TrimEnd('/')}/{azureAd["TenantId"]}/v2.0";
                    options.Audience = azureAd["Audience"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        RoleClaimType = "roles",
                        NameClaimType = "oid"
                    };
                }
                else
                {
                    // Offline stand-in issuer for tests: no Authority, so no metadata call.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = azureAd["Issuer"],
                        ValidAudience = azureAd["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testSigningKey)),
                        RoleClaimType = "roles",
                        NameClaimType = "oid"
                    };
                }
            });
        }

        services.AddScoped<EntraUserSynchronizer>();
        services.AddAuthorization();

        return services;
    }

    private static string SelectScheme(StringValues authorization, string? localIssuer, bool entraEnabled)
    {
        if (!entraEnabled)
        {
            return CivicFlowAuthSchemes.Local;
        }

        var issuer = ReadUnverifiedIssuer(authorization);
        if (issuer is null || string.Equals(issuer, localIssuer, StringComparison.Ordinal))
        {
            return CivicFlowAuthSchemes.Local;
        }

        return CivicFlowAuthSchemes.Entra;
    }

    /// <summary>
    /// Reads <c>iss</c> from the bearer payload without verifying the signature. The chosen handler
    /// still performs full validation, so this only decides which validator sees the token.
    /// </summary>
    private static string? ReadUnverifiedIssuer(StringValues authorization)
    {
        var header = authorization.ToString();
        if (string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["Bearer ".Length..].Trim();
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = Base64Url.DecodeFromChars(parts[1]);
            using var document = JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("iss", out var issuer)
                ? issuer.GetString()
                : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}

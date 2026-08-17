using System.Text;
using System.Text.Json.Serialization;
using CivicFlow.Api.Middleware;
using CivicFlow.Application.Abstractions;
using CivicFlow.Application.Admin;
using CivicFlow.Application.Auth;
using CivicFlow.Application.Catalog;
using CivicFlow.Application.Requests;
using CivicFlow.Infrastructure;
using CivicFlow.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCivicFlowInfrastructure(builder.Configuration);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });
builder.Services.AddAuthorization();

// Split hosting (frontend and API on different origins) needs CORS. Docker and local dev are
// same-origin, so the policy is only registered when an origin is configured.
const string FrontendCorsPolicy = "CivicFlowFrontend";
var frontendOrigin = builder.Configuration["Cors:AllowedOrigin"];
var hasFrontendOrigin = !string.IsNullOrWhiteSpace(frontendOrigin);
if (hasFrontendOrigin)
{
    builder.Services.AddCors(options => options.AddPolicy(
        FrontendCorsPolicy,
        policy => policy
            .WithOrigins(frontendOrigin!)
            .AllowAnyHeader()
            .AllowAnyMethod()));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
    await MigrateWithRetryAsync(db);
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (ShouldRedirectHttps())
{
    app.UseHttpsRedirection();
}

if (hasFrontendOrigin)
{
    app.UseCors(FrontendCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool ShouldRedirectHttps()
{
    var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    return string.IsNullOrEmpty(urls)
        || urls.Contains("https://", StringComparison.OrdinalIgnoreCase);
}

static async Task MigrateWithRetryAsync(CivicFlowDbContext db)
{
    const int maxAttempts = 10;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception) when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

public partial class Program;

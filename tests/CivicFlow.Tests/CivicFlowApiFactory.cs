using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CivicFlow.Tests;

public sealed class CivicFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _documentRoot;

    public CivicFlowApiFactory(string connectionString)
    {
        _connectionString = connectionString;
        _documentRoot = Path.Combine(Path.GetTempPath(), "civicflow-test-docs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_documentRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:CivicFlow", _connectionString);
        builder.UseSetting("Jwt:Issuer", "CivicFlow");
        builder.UseSetting("Jwt:Audience", "CivicFlow");
        builder.UseSetting("Jwt:SigningKey", "DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!");
        builder.UseSetting("Jwt:ExpiryMinutes", "480");
        builder.UseSetting("BlobStorage:LocalRoot", _documentRoot);
    }
}

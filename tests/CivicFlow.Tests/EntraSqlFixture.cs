using Microsoft.Data.SqlClient;

namespace CivicFlow.Tests;

/// <summary>
/// Separate database and xunit collection so Entra JIT rows never race the default SqlServer suite.
/// </summary>
public sealed class EntraSqlFixture : IAsyncLifetime
{
    public const string DefaultLocalDb =
        "Server=(localdb)\\MSSQLLocalDB;Database=CivicFlow_Entra_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public EntraApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        ConnectionString = ResolveConnectionString();
        Factory = new EntraApiFactory(ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    private static string ResolveConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__CivicFlow");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultLocalDb;
        }

        var builder = new SqlConnectionStringBuilder(configured);
        builder.InitialCatalog = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? "CivicFlow_Entra_Test"
            : builder.InitialCatalog + "_Entra";
        return builder.ConnectionString;
    }
}

[CollectionDefinition("EntraSql")]
public sealed class EntraSqlCollection : ICollectionFixture<EntraSqlFixture>;

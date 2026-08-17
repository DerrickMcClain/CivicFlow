namespace CivicFlow.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string DefaultLocalDb =
        "Server=(localdb)\\MSSQLLocalDB;Database=CivicFlow_Test;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public CivicFlowApiFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CivicFlow")
            ?? DefaultLocalDb;

        Factory = new CivicFlowApiFactory(ConnectionString);
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }
}

[CollectionDefinition("SqlServer")]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>;

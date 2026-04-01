using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace MapaTributario.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder("mongo:7")
        .Build();

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;
    protected IMongoDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var mongoClient = new MongoClient(_mongoContainer.GetConnectionString());
        Database = mongoClient.GetDatabase("test_db");

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
                    if (descriptor is not null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IMongoDatabase>(Database);

                    ConfigureTestServices(services);
                });
            });

        Client = Factory.CreateClient();

        await OnInitializedAsync();
    }

    /// <summary>
    /// Override to register additional test services (e.g., mocks).
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    /// <summary>
    /// Override to run setup logic after the test host is ready (e.g., seeding, authentication).
    /// </summary>
    protected virtual Task OnInitializedAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }
}

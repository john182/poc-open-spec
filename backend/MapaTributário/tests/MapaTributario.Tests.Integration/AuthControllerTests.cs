using System.Net;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;
using Testcontainers.MongoDb;

namespace MapaTributario.Tests.Integration;

public class AuthControllerTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
                    if (descriptor is not null)
                    {
                        services.Remove(descriptor);
                    }

                    var client = new MongoClient(_mongoContainer.GetConnectionString());
                    var database = client.GetDatabase("test_db");
                    services.AddSingleton<IMongoDatabase>(database);
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_ComDadosValidos_Retorna201()
    {
        var request = new RegisterRequest { Email = "new@test.com", Nome = "Novo Usuario", Senha = "password123" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
        body.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_ComEmailDuplicado_Retorna409()
    {
        var request = new RegisterRequest { Email = "dup@test.com", Nome = "User", Senha = "password123" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_Retorna200()
    {
        var registerReq = new RegisterRequest { Email = "login@test.com", Nome = "User", Senha = "password123" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest { Email = "login@test.com", Senha = "password123" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_Retorna401()
    {
        var request = new LoginRequest { Email = "nobody@test.com", Senha = "wrongpassword" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ComTokenValido_Retorna200()
    {
        var registerReq = new RegisterRequest { Email = "refresh@test.com", Nome = "User", Senha = "password123" };
        var registerResp = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshReq = new RefreshRequest { RefreshToken = tokens!.RefreshToken };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Retorna200()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

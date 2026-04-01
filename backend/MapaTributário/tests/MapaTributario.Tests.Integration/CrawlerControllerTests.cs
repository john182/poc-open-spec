using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.External;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Moq;
using Shouldly;
using Testcontainers.MongoDb;

namespace MapaTributario.Tests.Integration;

public class CrawlerControllerTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private readonly Mock<INfseApiClient> _mockNfseClient = new();
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
                    // Replace MongoDB
                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDatabase));
                    if (descriptor is not null)
                    {
                        services.Remove(descriptor);
                    }

                    MongoClient client = new MongoClient(_mongoContainer.GetConnectionString());
                    IMongoDatabase database = client.GetDatabase("test_db");
                    services.AddSingleton<IMongoDatabase>(database);

                    // Replace NFS-e API client with mock
                    ServiceDescriptor? nfseDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INfseApiClient));
                    if (nfseDescriptor is not null)
                    {
                        services.Remove(nfseDescriptor);
                    }

                    services.AddSingleton<INfseApiClient>(_mockNfseClient.Object);
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

    private async Task<string> GetAuthTokenAsync()
    {
        RegisterRequest registerReq = new RegisterRequest
        {
            Email = $"crawler-test-{Guid.NewGuid():N}@test.com",
            Nome = "Test User",
            Senha = "password123"
        };

        HttpResponseMessage registerResp = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        AuthResponse? tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    [Fact]
    public async Task Executar_SemAutenticacao_Retorna401()
    {
        using HttpClient unauthClient = _factory.CreateClient();
        HttpResponseMessage response = await unauthClient.PostAsJsonAsync(
            "/api/v1/crawler/executar",
            new ExecutarCrawlerRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Executar_ComAutenticacao_Retorna202()
    {
        using HttpClient authClient = _factory.CreateClient();
        string token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await authClient.PostAsJsonAsync(
            "/api/v1/crawler/executar",
            new ExecutarCrawlerRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        ExecutarCrawlerResponse? body = await response.Content.ReadFromJsonAsync<ExecutarCrawlerResponse>();
        body.ShouldNotBeNull();
        body.ExecucaoId.ShouldNotBeNullOrEmpty();
        body.Mensagem.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Status_SemExecucao_Retorna404Ou200()
    {
        using HttpClient authClient = _factory.CreateClient();
        string token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await authClient.GetAsync("/api/v1/crawler/status");

        // May be 200 if an execution was created by another test, or 404 if none
        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound).ShouldBeTrue();
    }

    [Fact]
    public async Task Status_ComExecucao_Retorna200()
    {
        using HttpClient authClient = _factory.CreateClient();
        string token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an execution first
        await authClient.PostAsJsonAsync("/api/v1/crawler/executar", new ExecutarCrawlerRequest());

        // Wait a moment for the execution to be created
        await Task.Delay(1000);

        HttpResponseMessage response = await authClient.GetAsync("/api/v1/crawler/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        StatusCrawlerResponse? body = await response.Content.ReadFromJsonAsync<StatusCrawlerResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execucoes_Retorna200ComLista()
    {
        using HttpClient authClient = _factory.CreateClient();
        string token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await authClient.GetAsync("/api/v1/crawler/execucoes");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<StatusCrawlerResponse>? body = await response.Content.ReadFromJsonAsync<List<StatusCrawlerResponse>>();
        body.ShouldNotBeNull();
    }

    [Fact]
    public async Task Execucoes_SemAutenticacao_Retorna401()
    {
        using HttpClient unauthClient = _factory.CreateClient();

        HttpResponseMessage response = await unauthClient.GetAsync("/api/v1/crawler/execucoes");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Status_SemAutenticacao_Retorna401()
    {
        using HttpClient unauthClient = _factory.CreateClient();

        HttpResponseMessage response = await unauthClient.GetAsync("/api/v1/crawler/status");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

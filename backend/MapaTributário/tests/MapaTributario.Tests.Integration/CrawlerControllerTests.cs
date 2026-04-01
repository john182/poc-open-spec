using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Integration;

public class CrawlerControllerTests : IntegrationTestBase
{
    private readonly Mock<INfseApiClient> _mockNfseClient = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Replace NFS-e API client with mock
        var nfseDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INfseApiClient));
        if (nfseDescriptor is not null)
        {
            services.Remove(nfseDescriptor);
        }

        services.AddSingleton<INfseApiClient>(_mockNfseClient.Object);
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var registerReq = new RegisterRequest
        {
            Email = $"crawler-test-{Guid.NewGuid():N}@test.com",
            Nome = "Test User",
            Senha = "password123"
        };

        var registerResp = await Client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    [Fact]
    public async Task Executar_SemAutenticacao_Retorna401()
    {
        using var unauthClient = Factory.CreateClient();
        var response = await unauthClient.PostAsJsonAsync(
            "/api/v1/crawler/executar",
            new ExecutarCrawlerRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Executar_ComAutenticacao_Retorna202()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.PostAsJsonAsync(
            "/api/v1/crawler/executar",
            new ExecutarCrawlerRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var body = await response.Content.ReadFromJsonAsync<ExecutarCrawlerResponse>();
        body.ShouldNotBeNull();
        body.Mensagem.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Status_SemExecucao_Retorna404Ou200()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.GetAsync("/api/v1/crawler/status");

        // May be 200 if an execution was created by another test, or 404 if none
        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound).ShouldBeTrue();
    }

    [Fact]
    public async Task Status_ComExecucao_Retorna200()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an execution first
        await authClient.PostAsJsonAsync("/api/v1/crawler/executar", new ExecutarCrawlerRequest());

        // Wait a moment for the execution to be created
        await Task.Delay(1000);

        var response = await authClient.GetAsync("/api/v1/crawler/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatusCrawlerResponse>();
        body.ShouldNotBeNull();
        body.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execucoes_Retorna200ComLista()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAuthTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.GetAsync("/api/v1/crawler/execucoes");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<StatusCrawlerResponse>>();
        body.ShouldNotBeNull();
    }

    [Fact]
    public async Task Execucoes_SemAutenticacao_Retorna401()
    {
        using var unauthClient = Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/crawler/execucoes");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Status_SemAutenticacao_Retorna401()
    {
        using var unauthClient = Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/crawler/status");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

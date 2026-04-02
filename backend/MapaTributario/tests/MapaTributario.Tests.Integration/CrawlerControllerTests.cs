using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Infrastructure.External;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Integration;

public class CrawlerControllerTests : IntegrationTestBase
{
    private readonly Mock<INfseApiClient> _mockNfseClient = new();
    private readonly Mock<ICertificadoStore> _mockCertificadoStore = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Replace NFS-e API client with mock
        var nfseDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(INfseApiClient));
        if (nfseDescriptor is not null)
        {
            services.Remove(nfseDescriptor);
        }

        services.AddSingleton<INfseApiClient>(_mockNfseClient.Object);

        // Replace CertificadoStore with mock that always has certificate
        _mockCertificadoStore.Setup(s => s.HasCertificate()).Returns(true);
        var certDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICertificadoStore));
        if (certDescriptor is not null)
        {
            services.Remove(certDescriptor);
        }

        services.AddSingleton<ICertificadoStore>(_mockCertificadoStore.Object);
    }

    private async Task<string> GetAdminTokenAsync()
    {
        // O AdminSeedService cria admin@admin.com / 12345678 no startup
        var loginReq = new LoginRequest
        {
            Email = "admin@admin.com",
            Senha = "12345678"
        };

        var loginResp = await Client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        var tokens = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    private async Task<string> GetUserTokenAsync()
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
        var token = await GetAdminTokenAsync();
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
    public async Task Executar_ComUsuarioComum_Retorna403()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetUserTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.PostAsJsonAsync(
            "/api/v1/crawler/executar",
            new ExecutarCrawlerRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Status_SemExecucao_Retorna200ComStatusNeutro()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAdminTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await authClient.GetAsync("/api/v1/crawler/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<StatusCrawlerResponse>();
        body.ShouldNotBeNull();
        body.Status.ShouldBe("NenhumaExecucao");
    }

    [Fact]
    public async Task Status_ComExecucao_Retorna200()
    {
        using var authClient = Factory.CreateClient();
        var token = await GetAdminTokenAsync();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an execution first
        await authClient.PostAsJsonAsync("/api/v1/crawler/executar", new ExecutarCrawlerRequest());

        // Wait for fire-and-forget to persist the execution
        await Task.Delay(3000);

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
        var token = await GetAdminTokenAsync();
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

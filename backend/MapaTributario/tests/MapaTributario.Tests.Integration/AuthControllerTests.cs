using System.Net;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using Shouldly;

namespace MapaTributario.Tests.Integration;

public class AuthControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task Register_ComDadosValidos_Retorna201()
    {
        var request = new RegisterRequest { Email = "new@test.com", Nome = "Novo Usuario", Senha = "password123" };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

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
        await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_Retorna200()
    {
        var registerReq = new RegisterRequest { Email = "login@test.com", Nome = "User", Senha = "password123" };
        await Client.PostAsJsonAsync("/api/v1/auth/register", registerReq);

        var loginReq = new LoginRequest { Email = "login@test.com", Senha = "password123" };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginReq);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_Retorna401()
    {
        var request = new LoginRequest { Email = "nobody@test.com", Senha = "wrongpassword" };

        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ComTokenValido_Retorna200()
    {
        var registerReq = new RegisterRequest { Email = "refresh@test.com", Nome = "User", Senha = "password123" };
        var registerResp = await Client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshReq = new RefreshRequest { RefreshToken = tokens!.RefreshToken };
        var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Retorna200()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

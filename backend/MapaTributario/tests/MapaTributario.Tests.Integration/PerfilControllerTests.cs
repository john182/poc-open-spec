using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Application.Perfil.Contracts;
using Shouldly;

namespace MapaTributario.Tests.Integration;

public class PerfilControllerTests : IntegrationTestBase
{
    private async Task<string> RegistrarEObterTokenAsync(string email = "perfil@test.com", string nome = "Usuario Teste", string senha = "password123")
    {
        var registerReq = new RegisterRequest { Email = email, Nome = nome, Senha = senha };
        var registerResp = await Client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        return tokens!.AccessToken;
    }

    private void ConfigurarAutorizacao(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task ObterPerfil_ComTokenValido_Retorna200()
    {
        var token = await RegistrarEObterTokenAsync();
        ConfigurarAutorizacao(token);

        var response = await Client.GetAsync("/api/v1/perfil");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PerfilResponse>();
        body.ShouldNotBeNull();
        body.Nome.ShouldBe("Usuario Teste");
        body.Email.ShouldBe("perfil@test.com");
        body.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ObterPerfil_SemToken_Retorna401()
    {
        var response = await Client.GetAsync("/api/v1/perfil");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AtualizarPerfil_SomenteNome_Retorna200ComNovoToken()
    {
        var token = await RegistrarEObterTokenAsync("atualnome@test.com");
        ConfigurarAutorizacao(token);

        var request = new AtualizarPerfilRequest { Nome = "Nome Atualizado" };
        var response = await Client.PutAsJsonAsync("/api/v1/perfil", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AtualizarPerfilResponse>();
        body.ShouldNotBeNull();
        body.Nome.ShouldBe("Nome Atualizado");
        body.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task AtualizarPerfil_NomeESenha_Retorna200()
    {
        var token = await RegistrarEObterTokenAsync("atualsenha@test.com", senha: "senhaoriginal123");
        ConfigurarAutorizacao(token);

        var request = new AtualizarPerfilRequest
        {
            Nome = "Nome Novo",
            SenhaAtual = "senhaoriginal123",
            NovaSenha = "novasenha456"
        };
        var response = await Client.PutAsJsonAsync("/api/v1/perfil", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AtualizarPerfilResponse>();
        body.ShouldNotBeNull();
        body.Nome.ShouldBe("Nome Novo");
    }

    [Fact]
    public async Task AtualizarPerfil_SenhaAtualIncorreta_Retorna400()
    {
        var token = await RegistrarEObterTokenAsync("senhaerrada@test.com");
        ConfigurarAutorizacao(token);

        var request = new AtualizarPerfilRequest
        {
            Nome = "Qualquer Nome",
            SenhaAtual = "senha_incorreta",
            NovaSenha = "novasenha456"
        };
        var response = await Client.PutAsJsonAsync("/api/v1/perfil", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPerfil_NomeVazio_Retorna400()
    {
        var token = await RegistrarEObterTokenAsync("nomevazio@test.com");
        ConfigurarAutorizacao(token);

        var request = new AtualizarPerfilRequest { Nome = "" };
        var response = await Client.PutAsJsonAsync("/api/v1/perfil", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AtualizarPerfil_VerificaPerfilAposAtualizacao_NomeReflete()
    {
        var token = await RegistrarEObterTokenAsync("verificar@test.com");
        ConfigurarAutorizacao(token);

        var updateRequest = new AtualizarPerfilRequest { Nome = "Nome Verificado" };
        var updateResponse = await Client.PutAsJsonAsync("/api/v1/perfil", updateRequest);
        var updateBody = await updateResponse.Content.ReadFromJsonAsync<AtualizarPerfilResponse>();

        // Usar novo token para verificar
        ConfigurarAutorizacao(updateBody!.AccessToken);
        var getResponse = await Client.GetAsync("/api/v1/perfil");
        var perfil = await getResponse.Content.ReadFromJsonAsync<PerfilResponse>();

        perfil.ShouldNotBeNull();
        perfil.Nome.ShouldBe("Nome Verificado");
    }
}

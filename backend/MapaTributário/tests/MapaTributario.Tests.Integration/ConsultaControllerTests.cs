using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MapaTributario.API.Application.Auth.Contracts;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shouldly;
using Testcontainers.MongoDb;

namespace MapaTributario.Tests.Integration;

public class ConsultaControllerTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private IMongoDatabase _database = null!;

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();

        var mongoClient = new MongoClient(_mongoContainer.GetConnectionString());
        _database = mongoClient.GetDatabase("test_db");

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

                    services.AddSingleton<IMongoDatabase>(_database);
                });
            });

        _client = _factory.CreateClient();

        await SeedTestDataAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mongoContainer.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Seed aliquotas for testing (estados, municipios and servicos are seeded by the app startup)
        var aliquotas = _database.GetCollection<Aliquota>("aliquotas");

        var testAliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise e desenvolvimento de sistemas", 2.0m, "2024-01", "NFS-e"),
            Aliquota.Create("3550308", "São Paulo", "010200", "01.02.00", "Programação", 3.0m, "2024-01", "NFS-e"),
            Aliquota.Create("3550308", "São Paulo", "010300", "01.03.00", "Processamento de dados", 5.0m, "2024-01", "NFS-e"),
            Aliquota.Create("3304557", "Rio de Janeiro", "010100", "01.01.00", "Análise e desenvolvimento de sistemas", 2.5m, "2024-01", "NFS-e"),
            Aliquota.Create("3304557", "Rio de Janeiro", "010200", "01.02.00", "Programação", 4.0m, "2024-02", "NFS-e")
        };

        await aliquotas.InsertManyAsync(testAliquotas);
    }

    private async Task AuthenticateAsync()
    {
        var registerReq = new RegisterRequest
        {
            Email = "consulta@test.com",
            Nome = "Consulta Test",
            Senha = "password123"
        };
        var registerResp = await _client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        var tokens = await registerResp.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
    }

    // --- Auth ---

    [Fact]
    public async Task GET_Estados_SemToken_Retorna401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/estados");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unauthClient.Dispose();
    }

    // --- Estados ---

    [Fact]
    public async Task GET_Estados_Retorna200_ComEstados()
    {
        var response = await _client.GetAsync("/api/v1/estados");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var estados = await response.Content.ReadFromJsonAsync<List<EstadoResponse>>();
        estados.ShouldNotBeNull();
        estados.Count.ShouldBe(27);
        estados.First().Sigla.ShouldNotBeNullOrEmpty();
        estados.First().Nome.ShouldNotBeNullOrEmpty();
        estados.First().Regiao.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GET_Estados_EstaoOrdenadosPorNome()
    {
        var response = await _client.GetAsync("/api/v1/estados");
        var estados = await response.Content.ReadFromJsonAsync<List<EstadoResponse>>();

        estados.ShouldNotBeNull();
        var nomes = estados.Select(e => e.Nome).ToList();
        // MongoDB uses binary sort by default (accent-insensitive ordering may differ from .NET)
        // Verify the list is returned sorted by checking adjacent pairs via MongoDB's own sort
        var nomesOrdenadosMongo = nomes.OrderBy(n => n, StringComparer.Ordinal).ToList();
        nomes.ShouldBe(nomesOrdenadosMongo);
    }

    // --- Municipios por UF ---

    [Fact]
    public async Task GET_MunicipiosPorUf_UfValida_Retorna200()
    {
        var response = await _client.GetAsync("/api/v1/estados/SP/municipios");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var municipios = await response.Content.ReadFromJsonAsync<List<MunicipioResponse>>();
        municipios.ShouldNotBeNull();
        municipios.Count.ShouldBeGreaterThan(0);
        municipios.All(m => m.SiglaEstado == "SP").ShouldBeTrue();
    }

    [Fact]
    public async Task GET_MunicipiosPorUf_UfInvalida_Retorna404()
    {
        var response = await _client.GetAsync("/api/v1/estados/XX/municipios");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_MunicipiosPorUf_SemToken_Retorna401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/estados/SP/municipios");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unauthClient.Dispose();
    }

    // --- Aliquotas por municipio ---

    [Fact]
    public async Task GET_AliquotasPorMunicipio_Retorna200_ComPaginacao()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(3);
        paginado.Pagina.ShouldBe(1);
        paginado.TamanhoPagina.ShouldBe(20);
        paginado.TotalItens.ShouldBe(3);
        paginado.TotalPaginas.ShouldBe(1);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_ComTamanhoPagina_LimitaResultados()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?tamanhoPagina=2&pagina=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(2);
        paginado.TotalItens.ShouldBe(3);
        paginado.TotalPaginas.ShouldBe(2);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_Pagina2_RetornaRestante()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?tamanhoPagina=2&pagina=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(1);
        paginado.Pagina.ShouldBe(2);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_FiltroCodigoServico_Funciona()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?codigoServico=01.01.00");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(1);
        paginado.Items[0].CodigoServicoFormatado.ShouldBe("01.01.00");
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_FiltroDescricao_Funciona()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?descricao=Programa");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(1);
        paginado.Items[0].DescricaoServico.ShouldContain("Programação");
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_FiltroAliquotaMinMax_Funciona()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?aliquotaMin=2.5&aliquotaMax=4.0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paginado = await response.Content.ReadFromJsonAsync<PaginatedResponse<AliquotaResponse>>();
        paginado.ShouldNotBeNull();
        paginado.Items.Count.ShouldBe(1);
        paginado.Items[0].ValorAliquota.ShouldBe(3.0m);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_MunicipioInexistente_Retorna404()
    {
        var response = await _client.GetAsync("/api/v1/municipios/9999999/aliquotas");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_TamanhoPaginaInvalido_Retorna400()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas?tamanhoPagina=0");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_AliquotasPorMunicipio_SemToken_Retorna401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/municipios/3550308/aliquotas");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unauthClient.Dispose();
    }

    // --- Detalhe aliquota ---

    [Fact]
    public async Task GET_DetalheAliquota_Existente_Retorna200()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas/01.01.00");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detalhe = await response.Content.ReadFromJsonAsync<AliquotaDetalheResponse>();
        detalhe.ShouldNotBeNull();
        detalhe.CodigoMunicipio.ShouldBe("3550308");
        detalhe.NomeMunicipio.ShouldBe("São Paulo");
        detalhe.CodigoServicoFormatado.ShouldBe("01.01.00");
        detalhe.ValorAliquota.ShouldBe(2.0m);
        detalhe.Competencia.ShouldBe("2024-01");
    }

    [Fact]
    public async Task GET_DetalheAliquota_CodigoSemPontos_Funciona()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas/010100");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var detalhe = await response.Content.ReadFromJsonAsync<AliquotaDetalheResponse>();
        detalhe.ShouldNotBeNull();
        detalhe.CodigoServicoFormatado.ShouldBe("01.01.00");
    }

    [Fact]
    public async Task GET_DetalheAliquota_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync("/api/v1/municipios/3550308/aliquotas/99.00.00");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_DetalheAliquota_MunicipioInexistente_Retorna404()
    {
        var response = await _client.GetAsync("/api/v1/municipios/9999999/aliquotas/01.01.00");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_DetalheAliquota_SemToken_Retorna401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/municipios/3550308/aliquotas/01.01.00");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unauthClient.Dispose();
    }
}

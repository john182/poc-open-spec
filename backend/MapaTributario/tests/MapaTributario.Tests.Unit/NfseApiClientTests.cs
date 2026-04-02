using System.Net;
using System.Text.Json;
using MapaTributario.API.Infrastructure.External;
using MapaTributario.API.Infrastructure.External.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class NfseApiClientTests
{
    private readonly Mock<ILogger<NfseApiClient>> _logger = new();

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    private NfseApiClient CriarSut(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        MockHttpMessageHandler mockHandler = new(handler);
        HttpClient httpClient = new(mockHandler)
        {
            BaseAddress = new Uri("https://adn.nfse.gov.br")
        };
        return new NfseApiClient(httpClient, _logger.Object);
    }

    private static HttpResponseMessage CriarRespostaJson<T>(T conteudo, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        string json = JsonSerializer.Serialize(conteudo);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    #region GetAliquotaAsync

    [Fact]
    public async Task Given_RespostaComSucesso_Should_RetornarAliquotaDesserializada()
    {
        // Arrange
        var respostaEsperada = new AliquotaNfseResponse
        {
            Aliquotas = new Dictionary<string, List<AliquotaItem>>
            {
                ["01.01.01.000"] = new List<AliquotaItem>
                {
                    new() { Incidencia = "SIM", Aliq = 5.0m, DtIni = new DateTime(2023, 10, 20), DtFim = null }
                }
            },
            Mensagem = "Alíquotas recuperadas com sucesso."
        };

        NfseApiClient sut = CriarSut(_ => CriarRespostaJson(respostaEsperada));

        // Act
        AliquotaNfseResponse? resultado = await sut.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01");

        // Assert
        resultado.ShouldNotBeNull();
        resultado.TemDados.ShouldBeTrue();
        resultado.Aliquotas!["01.01.01.000"].Count.ShouldBe(1);
        resultado.Aliquotas["01.01.01.000"][0].Aliq.ShouldBe(5.0m);
        resultado.Mensagem.ShouldBe("Alíquotas recuperadas com sucesso.");
    }

    [Fact]
    public async Task Given_Resposta404_Should_RetornarNuloParaAliquota()
    {
        // Arrange
        NfseApiClient sut = CriarSut(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        AliquotaNfseResponse? resultado = await sut.GetAliquotaAsync("9999999", "01.01.01", "2026-04-01");

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task Given_RespostaBadRequest_Should_RetornarNuloParaAliquota()
    {
        // Arrange
        NfseApiClient sut = CriarSut(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Código inválido")
        });

        // Act
        AliquotaNfseResponse? resultado = await sut.GetAliquotaAsync("invalido", "99.99.99", "2026-04-01");

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task Given_RespostaJsonInvalido_Should_RetornarNuloParaAliquota()
    {
        // Arrange
        NfseApiClient sut = CriarSut(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ json invalido !!!", System.Text.Encoding.UTF8, "application/json")
        });

        // Act
        AliquotaNfseResponse? resultado = await sut.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01");

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task Given_UrlConstruida_Should_ConterCodigoFormatadoCorretamente()
    {
        // Arrange
        string? urlCapturada = null;
        NfseApiClient sut = CriarSut(request =>
        {
            urlCapturada = request.RequestUri?.PathAndQuery;
            return CriarRespostaJson(new AliquotaNfseResponse
            {
                Aliquotas = new Dictionary<string, List<AliquotaItem>>(),
                Mensagem = "ok"
            });
        });

        // Act
        await sut.GetAliquotaAsync("3106200", "01.01.01", "2026-04-01");

        // Assert
        urlCapturada.ShouldNotBeNull();
        urlCapturada.ShouldContain("/parametrizacao/3106200/01.01.01.000/2026-04-01/aliquota");
    }

    #endregion

    #region GetConvenioAsync

    [Fact]
    public async Task Given_RespostaComSucesso_Should_RetornarConvenioDesserializado()
    {
        // Arrange
        var respostaEsperada = new ConvenioNfseResponse
        {
            ParametrosConvenio = new ParametrosConvenio
            {
                AderenteAmbienteNacional = 1,
                AderenteEmissorNacional = 0,
                SituacaoEmissaoPadraoContribuintesRFB = 1,
                AderenteMAN = 0,
                PermiteAproveitametoDeCreditos = true
            },
            Mensagem = "Parâmetros do convênio recuperados com sucesso."
        };

        NfseApiClient sut = CriarSut(_ => CriarRespostaJson(respostaEsperada));

        // Act
        ConvenioNfseResponse? resultado = await sut.GetConvenioAsync("3106200");

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Ativo.ShouldBeTrue();
        resultado.ParametrosConvenio.ShouldNotBeNull();
        resultado.ParametrosConvenio!.AderenteAmbienteNacional.ShouldBe(1);
        resultado.Mensagem.ShouldBe("Parâmetros do convênio recuperados com sucesso.");
    }

    [Fact]
    public async Task Given_Resposta404_Should_RetornarNuloParaConvenio()
    {
        // Arrange
        NfseApiClient sut = CriarSut(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        ConvenioNfseResponse? resultado = await sut.GetConvenioAsync("9999999");

        // Assert
        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task Given_RespostaErroServidor_Should_LancarExcecaoParaConvenio()
    {
        // Arrange
        NfseApiClient sut = CriarSut(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        });

        // Act & Assert
        await Should.ThrowAsync<HttpRequestException>(
            () => sut.GetConvenioAsync("3106200"));
    }

    #endregion

    #region FormatarCodigoServico

    [Theory]
    [InlineData("01.01.01", "01.01.01.000")]
    [InlineData("01.01.00", "01.01.00.000")]
    [InlineData("010101", "01.01.01.000")]
    [InlineData("010101000", "01.01.01.000")]
    [InlineData("01.01.01.000", "01.01.01.000")]
    [InlineData("010101001", "01.01.01.001")]
    public void Given_CodigoServico_Should_FormatarCorretamente(string entrada, string esperado)
    {
        // Act
        string resultado = NfseApiClient.FormatarCodigoServico(entrada);

        // Assert
        resultado.ShouldBe(esperado);
    }

    #endregion
}

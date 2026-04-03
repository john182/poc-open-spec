using FluentResults;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ConfiguracaoCrawlerAppServiceTests
{
    private readonly Mock<IConfiguracaoCrawlerRepository> _repository = new();
    private readonly Mock<ILogger<ConfiguracaoCrawlerAppService>> _logger = new();
    private readonly ConfiguracaoCrawlerAppService _sut;

    public ConfiguracaoCrawlerAppServiceTests()
    {
        _sut = new ConfiguracaoCrawlerAppService(_repository.Object, _logger.Object);
    }

    [Fact]
    public async Task Given_ConfiguracaoExistente_Should_RetornarConfiguracaoAtual()
    {
        // Arrange
        var configuracao = ConfiguracaoCrawler.CriarPadrao();
        configuracao.SetId("test-id");

        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync(configuracao);

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.ObterConfiguracaoAtualAsync();

        // Assert
        resultado.IsSuccess.ShouldBeTrue();

        ConfiguracaoCrawlerResponse resposta = resultado.Value;
        resposta.Id.ShouldBe("test-id");
        resposta.CronSchedule.ShouldBe("0 2 * * *");
        resposta.LimiteRequisicoesPorSegundo.ShouldBe(50);
        resposta.LimiteDiarioRequisicoes.ShouldBe(200000);
        resposta.TamanhoLoteCertificado.ShouldBe(500);
        resposta.PausaLoteSegundos.ShouldBe(0);
        resposta.TamanhoLoteMongo.ShouldBe(50);
        resposta.MaxTentativas.ShouldBe(3);
        resposta.LimiteParadaAntecipada.ShouldBe(9);
        resposta.MaxDesdobramento.ShouldBe(20);
        resposta.MaxDetalhamento.ShouldBe(99);
        resposta.MaxFalhasConsecutivasDetalhamento.ShouldBe(2);
        resposta.MaxFalhasConsecutivasDesdobramento.ShouldBe(2);
        resposta.MaxItensParalelos.ShouldBe(20);
        resposta.MaxUfsParalelas.ShouldBe(5);
        resposta.CodigosSondagem.ShouldNotBeEmpty();
        resposta.ValidadeDiasProcessamento.ShouldBe(7);
        resposta.CircuitBreakerLimiarErroPercent.ShouldBe(50);
        resposta.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(60);
        resposta.CircuitBreakerPausaSegundos.ShouldBe(300);
        resposta.CircuitBreakerAmostraMinima.ShouldBe(10);
        resposta.Ativo.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_NenhumaConfiguracaoAtiva_Should_RetornarNotFoundError()
    {
        // Arrange
        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync((ConfiguracaoCrawler?)null);

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.ObterConfiguracaoAtualAsync();

        // Assert
        resultado.IsFailed.ShouldBeTrue();
        resultado.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Given_RequestValido_Should_AtualizarConfiguracaoCompleta()
    {
        // Arrange
        var configuracao = ConfiguracaoCrawler.CriarPadrao();
        configuracao.SetId("config-123");

        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync(configuracao);

        var request = new AtualizarConfiguracaoCrawlerRequest
        {
            CronSchedule = "0 3 * * *",
            LimiteRequisicoesPorSegundo = 20,
            LimiteDiarioRequisicoes = 60000,
            TamanhoLoteCertificado = 300,
            PausaLoteSegundos = 10,
            TamanhoLoteMongo = 100,
            MaxTentativas = 5,
            LimiteParadaAntecipada = 12,
            MaxDesdobramento = 30,
            MaxDetalhamento = 50,
            MaxFalhasConsecutivasDetalhamento = 4,
            MaxFalhasConsecutivasDesdobramento = 4,
            MaxItensParalelos = 15,
            MaxUfsParalelas = 8,
            CodigosSondagem = new List<string> { "01.01.01", "07.02.01" },
            ValidadeDiasProcessamento = 14,
            CircuitBreakerLimiarErroPercent = 70,
            CircuitBreakerJanelaAvaliacaoSegundos = 120,
            CircuitBreakerPausaSegundos = 600,
            CircuitBreakerAmostraMinima = 20,
            Ativo = false
        };

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.AtualizarConfiguracaoAsync(request);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();

        ConfiguracaoCrawlerResponse resposta = resultado.Value;
        resposta.CronSchedule.ShouldBe("0 3 * * *");
        resposta.LimiteRequisicoesPorSegundo.ShouldBe(20);
        resposta.LimiteDiarioRequisicoes.ShouldBe(60000);
        resposta.TamanhoLoteCertificado.ShouldBe(300);
        resposta.PausaLoteSegundos.ShouldBe(10);
        resposta.TamanhoLoteMongo.ShouldBe(100);
        resposta.MaxTentativas.ShouldBe(5);
        resposta.LimiteParadaAntecipada.ShouldBe(12);
        resposta.MaxDesdobramento.ShouldBe(30);
        resposta.MaxDetalhamento.ShouldBe(50);
        resposta.MaxFalhasConsecutivasDetalhamento.ShouldBe(4);
        resposta.MaxFalhasConsecutivasDesdobramento.ShouldBe(4);
        resposta.MaxItensParalelos.ShouldBe(15);
        resposta.MaxUfsParalelas.ShouldBe(8);
        resposta.CodigosSondagem.Count.ShouldBe(2);
        resposta.ValidadeDiasProcessamento.ShouldBe(14);
        resposta.CircuitBreakerLimiarErroPercent.ShouldBe(70);
        resposta.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(120);
        resposta.CircuitBreakerPausaSegundos.ShouldBe(600);
        resposta.CircuitBreakerAmostraMinima.ShouldBe(20);
        resposta.Ativo.ShouldBeFalse();

        _repository.Verify(r => r.AtualizarAsync(It.IsAny<ConfiguracaoCrawler>()), Times.Once);
    }

    [Fact]
    public async Task Given_NenhumaConfiguracaoAtiva_AtualizarCompleto_Should_RetornarNotFoundError()
    {
        // Arrange
        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync((ConfiguracaoCrawler?)null);

        var request = new AtualizarConfiguracaoCrawlerRequest
        {
            CronSchedule = "0 3 * * *",
            LimiteRequisicoesPorSegundo = 20,
            LimiteDiarioRequisicoes = 60000,
            TamanhoLoteCertificado = 300,
            PausaLoteSegundos = 10,
            TamanhoLoteMongo = 100,
            MaxTentativas = 5,
            LimiteParadaAntecipada = 12,
            MaxDesdobramento = 30,
            MaxDetalhamento = 50,
            MaxFalhasConsecutivasDetalhamento = 4,
            MaxFalhasConsecutivasDesdobramento = 4,
            MaxItensParalelos = 15,
            MaxUfsParalelas = 8,
            CodigosSondagem = new List<string> { "01.01.01" },
            ValidadeDiasProcessamento = 14,
            CircuitBreakerLimiarErroPercent = 70,
            CircuitBreakerJanelaAvaliacaoSegundos = 120,
            CircuitBreakerPausaSegundos = 600,
            CircuitBreakerAmostraMinima = 20,
            Ativo = false
        };

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.AtualizarConfiguracaoAsync(request);

        // Assert
        resultado.IsFailed.ShouldBeTrue();
        resultado.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();

        _repository.Verify(r => r.AtualizarAsync(It.IsAny<ConfiguracaoCrawler>()), Times.Never);
    }

    [Fact]
    public async Task Given_RequestParcialValido_Should_AtualizarApenasCamposInformados()
    {
        // Arrange
        var configuracao = ConfiguracaoCrawler.CriarPadrao();
        configuracao.SetId("config-parcial");

        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync(configuracao);

        var request = new AtualizarParcialConfiguracaoCrawlerRequest
        {
            CronSchedule = "0 4 * * *",
            MaxTentativas = 7
        };

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.AtualizarParcialmenteAsync(request);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();

        ConfiguracaoCrawlerResponse resposta = resultado.Value;

        // Campos alterados
        resposta.CronSchedule.ShouldBe("0 4 * * *");
        resposta.MaxTentativas.ShouldBe(7);

        // Campos inalterados (valores padrão)
        resposta.LimiteRequisicoesPorSegundo.ShouldBe(50);
        resposta.LimiteDiarioRequisicoes.ShouldBe(200000);
        resposta.TamanhoLoteCertificado.ShouldBe(500);
        resposta.PausaLoteSegundos.ShouldBe(0);
        resposta.TamanhoLoteMongo.ShouldBe(50);
        resposta.LimiteParadaAntecipada.ShouldBe(9);
        resposta.MaxDesdobramento.ShouldBe(20);
        resposta.MaxDetalhamento.ShouldBe(99);
        resposta.MaxFalhasConsecutivasDetalhamento.ShouldBe(2);
        resposta.MaxFalhasConsecutivasDesdobramento.ShouldBe(2);
        resposta.MaxItensParalelos.ShouldBe(20);
        resposta.MaxUfsParalelas.ShouldBe(5);
        resposta.ValidadeDiasProcessamento.ShouldBe(7);
        resposta.CircuitBreakerLimiarErroPercent.ShouldBe(50);
        resposta.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(60);
        resposta.CircuitBreakerPausaSegundos.ShouldBe(300);
        resposta.CircuitBreakerAmostraMinima.ShouldBe(10);
        resposta.Ativo.ShouldBeTrue();

        _repository.Verify(r => r.AtualizarAsync(It.IsAny<ConfiguracaoCrawler>()), Times.Once);
    }

    [Fact]
    public async Task Given_NenhumaConfiguracaoAtiva_AtualizarParcial_Should_RetornarNotFoundError()
    {
        // Arrange
        _repository
            .Setup(r => r.ObterAtualAsync())
            .ReturnsAsync((ConfiguracaoCrawler?)null);

        var request = new AtualizarParcialConfiguracaoCrawlerRequest
        {
            CronSchedule = "0 4 * * *"
        };

        // Act
        Result<ConfiguracaoCrawlerResponse> resultado = await _sut.AtualizarParcialmenteAsync(request);

        // Assert
        resultado.IsFailed.ShouldBeTrue();
        resultado.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();

        _repository.Verify(r => r.AtualizarAsync(It.IsAny<ConfiguracaoCrawler>()), Times.Never);
    }
}

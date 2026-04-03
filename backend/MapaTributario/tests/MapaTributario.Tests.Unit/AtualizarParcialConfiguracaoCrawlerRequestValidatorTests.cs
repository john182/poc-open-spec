using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Crawler.Validators;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class AtualizarParcialConfiguracaoCrawlerRequestValidatorTests
{
    private readonly AtualizarParcialConfiguracaoCrawlerRequestValidator _validador = new();

    private static AtualizarParcialConfiguracaoCrawlerRequest CriarRequestVazio()
    {
        return new AtualizarParcialConfiguracaoCrawlerRequest();
    }

    // ── Regra: ao menos um campo preenchido ─────────────────────────

    [Fact]
    public async Task Given_NenhumCampoPreenchido_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage.Contains("ao menos um campo"));
    }

    // ── CronSchedule ────────────────────────────────────────────────

    [Fact]
    public async Task Given_ApenaCronScheduleValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CronSchedule = "0 */6 * * *";

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_CronScheduleInvalido_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CronSchedule = "invalido";

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CronSchedule");
    }

    [Fact]
    public async Task Given_CronScheduleVazio_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CronSchedule = "";

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CronSchedule");
    }

    // ── LimiteRequisicoesPorSegundo ─────────────────────────────────

    [Fact]
    public async Task Given_ApenaLimiteRequisicoesValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.LimiteRequisicoesPorSegundo = 10;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Given_LimiteRequisicoesForaDoLimite_Should_FalharValidacao(int limite)
    {
        // Arrange
        var request = CriarRequestVazio();
        request.LimiteRequisicoesPorSegundo = limite;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteRequisicoesPorSegundo.Value");
    }

    // ── LimiteDiarioRequisicoes ─────────────────────────────────────────────

    [Fact]
    public async Task Given_ApenaLimiteDiarioRequisicoesValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.LimiteDiarioRequisicoes = 1000;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500001)]
    public async Task Given_LimiteDiarioRequisicoesForaDoLimite_Should_FalharValidacao(int limiteDiario)
    {
        // Arrange
        var request = CriarRequestVazio();
        request.LimiteDiarioRequisicoes = limiteDiario;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteDiarioRequisicoes.Value");
    }

    // ── CodigosSondagem ─────────────────────────────────────────────

    [Fact]
    public async Task Given_CodigosSondagemVazio_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CodigosSondagem = [];

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CodigosSondagem");
    }

    [Fact]
    public async Task Given_CodigosSondagemFormatoInvalido_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CodigosSondagem = ["invalido"];

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage.Contains("formato XX.XX.XX"));
    }

    [Fact]
    public async Task Given_CodigosSondagemValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CodigosSondagem = ["01.01.01"];

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_CodigosSondagemNulo_Should_NaoValidarRegrasDeFormato()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CodigosSondagem = null;
        request.LimiteRequisicoesPorSegundo = 10; // garante ao menos um campo

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── MaxTentativas ───────────────────────────────────────────────

    [Fact]
    public async Task Given_MaxTentativasMaiorQue10_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.MaxTentativas = 11;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxTentativas.Value");
    }

    [Fact]
    public async Task Given_MaxTentativasValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.MaxTentativas = 5;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── ValidadeDiasProcessamento ───────────────────────────────────

    [Fact]
    public async Task Given_ValidadeDiasMaiorQue365_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.ValidadeDiasProcessamento = 366;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "ValidadeDiasProcessamento.Value");
    }

    // ── CircuitBreakerLimiarErroPercent ──────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Given_CircuitBreakerLimiarForaDe1a100_Should_FalharValidacao(int limiar)
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CircuitBreakerLimiarErroPercent = limiar;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerLimiarErroPercent.Value");
    }

    [Fact]
    public async Task Given_CircuitBreakerLimiarValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CircuitBreakerLimiarErroPercent = 50;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── Combinações de campos ───────────────────────────────────────

    [Fact]
    public async Task Given_ApenaAtivoPreenchido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.Ativo = true;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_VariosCamposValidos_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CronSchedule = "0 */6 * * *";
        request.LimiteRequisicoesPorSegundo = 10;
        request.LimiteDiarioRequisicoes = 1000;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_CampoValidoECampoInvalido_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CronSchedule = "0 */6 * * *";
        request.LimiteRequisicoesPorSegundo = 0; // inválido

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteRequisicoesPorSegundo.Value");
    }

    // ── CircuitBreakerSegundos parcial ──────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task Given_CircuitBreakerJanelaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CircuitBreakerJanelaAvaliacaoSegundos = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerJanelaAvaliacaoSegundos.Value");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task Given_CircuitBreakerPausaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestVazio();
        request.CircuitBreakerPausaSegundos = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerPausaSegundos.Value");
    }
}

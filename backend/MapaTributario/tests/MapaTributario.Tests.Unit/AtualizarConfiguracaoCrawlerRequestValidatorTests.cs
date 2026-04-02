using MapaTributario.API.Application.Crawler.Contracts;
using MapaTributario.API.Application.Crawler.Validators;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class AtualizarConfiguracaoCrawlerRequestValidatorTests
{
    private readonly AtualizarConfiguracaoCrawlerRequestValidator _validador = new();

    private static AtualizarConfiguracaoCrawlerRequest CriarRequestValido()
    {
        return new AtualizarConfiguracaoCrawlerRequest
        {
            CronSchedule = "0 */6 * * *",
            LimiteRequisicoesPorSegundo = 10,
            OrcamentoDiario = 1000,
            TamanhoLoteCertificado = 50,
            PausaLoteSegundos = 5,
            TamanhoLoteMongo = 100,
            MaxTentativas = 3,
            LimiteParadaAntecipada = 10,
            MaxDesdobramento = 5,
            MaxDetalhamento = 50,
            MaxFalhasConsecutivasDetalhamento = 5,
            MaxFalhasConsecutivasDesdobramento = 5,
            MaxItensParalelos = 10,
            CodigosSondagem = ["01.01.01", "02.02.02"],
            ValidadeDiasProcessamento = 30,
            CircuitBreakerLimiarErroPercent = 50,
            CircuitBreakerJanelaAvaliacaoSegundos = 60,
            CircuitBreakerPausaSegundos = 30,
            CircuitBreakerAmostraMinima = 10,
            Ativo = true
        };
    }

    // ── CronSchedule ────────────────────────────────────────────────

    [Fact]
    public async Task Given_RequestValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestValido();

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Given_CronScheduleVazioOuNulo_Should_FalharValidacao(string? cronSchedule)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CronSchedule = cronSchedule!;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CronSchedule");
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("* * *")]
    [InlineData("* * * *")]
    public async Task Given_CronScheduleFormatoInvalido_Should_FalharValidacao(string cronSchedule)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CronSchedule = cronSchedule;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CronSchedule");
    }

    // ── LimiteRequisicoesPorSegundo ─────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Given_LimiteRequisicoesMenorOuIgualZero_Should_FalharValidacao(int limite)
    {
        // Arrange
        var request = CriarRequestValido();
        request.LimiteRequisicoesPorSegundo = limite;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteRequisicoesPorSegundo");
    }

    [Fact]
    public async Task Given_LimiteRequisicoesMaiorQue100_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.LimiteRequisicoesPorSegundo = 101;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteRequisicoesPorSegundo");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Given_LimiteRequisicoesNosLimites_Should_PassarValidacao(int limite)
    {
        // Arrange
        var request = CriarRequestValido();
        request.LimiteRequisicoesPorSegundo = limite;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── OrcamentoDiario ─────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Given_OrcamentoDiarioMenorOuIgualZero_Should_FalharValidacao(int orcamento)
    {
        // Arrange
        var request = CriarRequestValido();
        request.OrcamentoDiario = orcamento;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "OrcamentoDiario");
    }

    [Fact]
    public async Task Given_OrcamentoDiarioMaiorQue500000_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.OrcamentoDiario = 500001;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "OrcamentoDiario");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500000)]
    public async Task Given_OrcamentoDiarioNosLimites_Should_PassarValidacao(int orcamento)
    {
        // Arrange
        var request = CriarRequestValido();
        request.OrcamentoDiario = orcamento;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── CodigosSondagem ─────────────────────────────────────────────

    [Fact]
    public async Task Given_CodigosSondagemVazio_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.CodigosSondagem = [];

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CodigosSondagem");
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("1.1.1")]
    [InlineData("001.01.01")]
    [InlineData("01-01-01")]
    public async Task Given_CodigoSondagemFormatoInvalido_Should_FalharValidacao(string codigo)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CodigosSondagem = [codigo];

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage.Contains("formato XX.XX.XX"));
    }

    [Fact]
    public async Task Given_CodigoSondagemFormatoValido_Should_PassarValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.CodigosSondagem = ["01.01.01", "99.99.99"];

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
        var request = CriarRequestValido();
        request.MaxTentativas = 11;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxTentativas");
    }

    [Fact]
    public async Task Given_MaxTentativasZero_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxTentativas = 0;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxTentativas");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Given_MaxTentativasNosLimites_Should_PassarValidacao(int maxTentativas)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxTentativas = maxTentativas;

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
        var request = CriarRequestValido();
        request.ValidadeDiasProcessamento = 366;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "ValidadeDiasProcessamento");
    }

    [Fact]
    public async Task Given_ValidadeDiasZero_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.ValidadeDiasProcessamento = 0;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "ValidadeDiasProcessamento");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public async Task Given_ValidadeDiasNosLimites_Should_PassarValidacao(int dias)
    {
        // Arrange
        var request = CriarRequestValido();
        request.ValidadeDiasProcessamento = dias;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── CircuitBreakerLimiarErroPercent ──────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Given_CircuitBreakerLimiarForaDe1a100_Should_FalharValidacao(int limiar)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CircuitBreakerLimiarErroPercent = limiar;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerLimiarErroPercent");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(50)]
    public async Task Given_CircuitBreakerLimiarEntre1e100_Should_PassarValidacao(int limiar)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CircuitBreakerLimiarErroPercent = limiar;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── TamanhoLoteCertificado ──────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task Given_TamanhoLoteCertificadoForaDoLimite_Should_FalharValidacao(int tamanho)
    {
        // Arrange
        var request = CriarRequestValido();
        request.TamanhoLoteCertificado = tamanho;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "TamanhoLoteCertificado");
    }

    // ── PausaLoteSegundos ───────────────────────────────────────────

    [Fact]
    public async Task Given_PausaLoteSegundosNegativo_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.PausaLoteSegundos = -1;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "PausaLoteSegundos");
    }

    [Fact]
    public async Task Given_PausaLoteSegundosMaiorQue300_Should_FalharValidacao()
    {
        // Arrange
        var request = CriarRequestValido();
        request.PausaLoteSegundos = 301;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "PausaLoteSegundos");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public async Task Given_PausaLoteSegundosNosLimites_Should_PassarValidacao(int pausa)
    {
        // Arrange
        var request = CriarRequestValido();
        request.PausaLoteSegundos = pausa;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── TamanhoLoteMongo ────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Given_TamanhoLoteMongoForaDoLimite_Should_FalharValidacao(int tamanho)
    {
        // Arrange
        var request = CriarRequestValido();
        request.TamanhoLoteMongo = tamanho;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "TamanhoLoteMongo");
    }

    // ── MaxDesdobramento ────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Given_MaxDesdobramentoForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxDesdobramento = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxDesdobramento");
    }

    // ── MaxDetalhamento ─────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public async Task Given_MaxDetalhamentoForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxDetalhamento = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxDetalhamento");
    }

    // ── MaxFalhasConsecutivas ───────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Given_MaxFalhasConsecutivasDetalhamentoForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxFalhasConsecutivasDetalhamento = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxFalhasConsecutivasDetalhamento");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Given_MaxFalhasConsecutivasDesdobramentoForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxFalhasConsecutivasDesdobramento = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxFalhasConsecutivasDesdobramento");
    }

    // ── MaxItensParalelos ───────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Given_MaxItensParalelosForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.MaxItensParalelos = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "MaxItensParalelos");
    }

    // ── LimiteParadaAntecipada ──────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Given_LimiteParadaAntecipadaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.LimiteParadaAntecipada = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "LimiteParadaAntecipada");
    }

    // ── CircuitBreakerSegundos ──────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task Given_CircuitBreakerJanelaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CircuitBreakerJanelaAvaliacaoSegundos = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerJanelaAvaliacaoSegundos");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3601)]
    public async Task Given_CircuitBreakerPausaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CircuitBreakerPausaSegundos = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerPausaSegundos");
    }

    // ── CircuitBreakerAmostraMinima ─────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task Given_CircuitBreakerAmostraMinimaForaDoLimite_Should_FalharValidacao(int valor)
    {
        // Arrange
        var request = CriarRequestValido();
        request.CircuitBreakerAmostraMinima = valor;

        // Act
        var resultado = await _validador.ValidateAsync(request);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "CircuitBreakerAmostraMinima");
    }
}

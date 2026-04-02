using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Validators;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class AliquotaQueryParamsValidatorTests
{
    private readonly AliquotaQueryParamsValidator _validador = new();

    // ── Cenário válido ──────────────────────────────────────────────

    [Fact]
    public async Task Given_ParametrosPadrao_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams();

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_TodosParametrosValidos_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            Pagina = 1,
            TamanhoPagina = 50,
            AliquotaMin = 2.0m,
            AliquotaMax = 5.0m
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── Pagina ──────────────────────────────────────────────────────

    [Fact]
    public async Task Given_PaginaZero_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { Pagina = 0 };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "Pagina");
    }

    [Fact]
    public async Task Given_PaginaNegativa_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { Pagina = -1 };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "Pagina");
    }

    [Fact]
    public async Task Given_PaginaUm_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { Pagina = 1 };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── TamanhoPagina ───────────────────────────────────────────────

    [Fact]
    public async Task Given_TamanhoPaginaZero_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { TamanhoPagina = 0 };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "TamanhoPagina");
    }

    [Fact]
    public async Task Given_TamanhoPaginaMaiorQue100_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { TamanhoPagina = 101 };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "TamanhoPagina");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Given_TamanhoPaginaNosLimites_Should_PassarValidacao(int tamanho)
    {
        // Arrange
        var parametros = new AliquotaQueryParams { TamanhoPagina = tamanho };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── AliquotaMin ─────────────────────────────────────────────────

    [Fact]
    public async Task Given_AliquotaMinNegativa_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { AliquotaMin = -1m };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "AliquotaMin");
    }

    [Fact]
    public async Task Given_AliquotaMinZero_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { AliquotaMin = 0m };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── AliquotaMax ─────────────────────────────────────────────────

    [Fact]
    public async Task Given_AliquotaMaxNegativa_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { AliquotaMax = -1m };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == "AliquotaMax");
    }

    [Fact]
    public async Task Given_AliquotaMaxZero_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams { AliquotaMax = 0m };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    // ── AliquotaMin > AliquotaMax ───────────────────────────────────

    [Fact]
    public async Task Given_AliquotaMinMaiorQueMax_Should_FalharValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            AliquotaMin = 10m,
            AliquotaMax = 5m
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.ErrorMessage.Contains("mínima não pode ser maior"));
    }

    [Fact]
    public async Task Given_AliquotaMinIgualMax_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            AliquotaMin = 5m,
            AliquotaMax = 5m
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_AliquotaMinMenorQueMax_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            AliquotaMin = 2m,
            AliquotaMax = 5m
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_ApenaAliquotaMinSemMax_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            AliquotaMin = 5m,
            AliquotaMax = null
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Given_ApenaAliquotaMaxSemMin_Should_PassarValidacao()
    {
        // Arrange
        var parametros = new AliquotaQueryParams
        {
            AliquotaMin = null,
            AliquotaMax = 5m
        };

        // Act
        var resultado = await _validador.ValidateAsync(parametros);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }
}

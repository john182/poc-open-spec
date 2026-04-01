using MapaTributario.API.Application.Consulta;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CodigoServicoNormalizerTests
{
    [Theory]
    [InlineData("01.02.00", "010200")]
    [InlineData("14.01.00", "140100")]
    [InlineData("07.22.00", "072200")]
    public void RemoverPontos_CodigoComPontos_RetornaSemPontos(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.RemoverPontos(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("010200")]
    [InlineData("140100")]
    public void RemoverPontos_CodigoSemPontos_RetornaMesmoValor(string entrada)
    {
        var resultado = CodigoServicoNormalizer.RemoverPontos(entrada);
        resultado.ShouldBe(entrada);
    }

    [Fact]
    public void RemoverPontos_Null_RetornaVazio()
    {
        var resultado = CodigoServicoNormalizer.RemoverPontos(null!);
        resultado.ShouldBe(string.Empty);
    }

    [Fact]
    public void RemoverPontos_Vazio_RetornaVazio()
    {
        var resultado = CodigoServicoNormalizer.RemoverPontos("");
        resultado.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("010200", "01.02.00")]
    [InlineData("140100", "14.01.00")]
    [InlineData("072200", "07.22.00")]
    public void Formatar_CodigoSemPontos_RetornaFormatado(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.Formatar(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("01.02.00", "01.02.00")]
    public void Formatar_CodigoComPontos_RetornaFormatado(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.Formatar(entrada);
        resultado.ShouldBe(esperado);
    }

    [Fact]
    public void Formatar_CodigoComTamanhoInvalido_RetornaOriginal()
    {
        var resultado = CodigoServicoNormalizer.Formatar("12345");
        resultado.ShouldBe("12345");
    }

    [Fact]
    public void Formatar_Null_RetornaVazio()
    {
        var resultado = CodigoServicoNormalizer.Formatar(null!);
        resultado.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("01.02.00", "010200")]
    [InlineData("010200", "010200")]
    [InlineData("14.01.00", "140100")]
    public void Normalizar_FormatoValido_RetornaSemPontos(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.Normalizar(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("01.02")]
    [InlineData("01.AB.00")]
    public void Normalizar_FormatoInvalido_RetornaVazio(string? entrada)
    {
        var resultado = CodigoServicoNormalizer.Normalizar(entrada!);
        resultado.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("01.02.00", true)]
    [InlineData("010200", true)]
    [InlineData("14.01.00", true)]
    [InlineData("140100", true)]
    public void EhValido_FormatoCorreto_RetornaTrue(string entrada, bool esperado)
    {
        var resultado = CodigoServicoNormalizer.EhValido(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("abc", false)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("01.02", false)]
    public void EhValido_FormatoIncorreto_RetornaFalse(string? entrada, bool esperado)
    {
        var resultado = CodigoServicoNormalizer.EhValido(entrada!);
        resultado.ShouldBe(esperado);
    }

    // --- NormalizarPrefixo ---

    [Theory]
    [InlineData("01", "01")]
    [InlineData("14", "14")]
    public void NormalizarPrefixo_Grupo2Digitos_RetornaDigitos(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.NormalizarPrefixo(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("01.01", "0101")]
    [InlineData("0101", "0101")]
    [InlineData("14.01", "1401")]
    public void NormalizarPrefixo_SubItem4Digitos_RetornaDigitos(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.NormalizarPrefixo(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("01.01.00", "010100")]
    [InlineData("010100", "010100")]
    [InlineData("14.01.00", "140100")]
    public void NormalizarPrefixo_CodigoCompleto6Digitos_RetornaDigitos(string entrada, string esperado)
    {
        var resultado = CodigoServicoNormalizer.NormalizarPrefixo(entrada);
        resultado.ShouldBe(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("abc")]
    [InlineData("1")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("0A")]
    [InlineData("01.0A")]
    public void NormalizarPrefixo_FormatoInvalido_RetornaVazio(string? entrada)
    {
        var resultado = CodigoServicoNormalizer.NormalizarPrefixo(entrada!);
        resultado.ShouldBe(string.Empty);
    }
}

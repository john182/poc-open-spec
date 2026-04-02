using MapaTributario.API.Application.Errors;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CrawlerDesativadoErrorTests
{
    [Fact]
    public void Given_Instanciado_Should_ConterMensagemCorreta()
    {
        // Arrange & Act
        var erro = new CrawlerDesativadoError();

        // Assert
        erro.Message.ShouldBe("Crawler desativado pela configuração atual");
    }
}

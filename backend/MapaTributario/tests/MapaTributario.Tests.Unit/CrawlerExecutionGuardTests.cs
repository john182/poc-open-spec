using MapaTributario.API.Application.Crawler;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class CrawlerExecutionGuardTests
{
    [Fact]
    public void Given_EstadoInicial_Should_NaoEstarExecutando()
    {
        // Arrange
        var guarda = new CrawlerExecutionGuard();

        // Act
        var estaExecutando = guarda.IsRunning;

        // Assert
        estaExecutando.ShouldBeFalse();
    }

    [Fact]
    public void Given_AposTryAcquire_Should_EstarExecutando()
    {
        // Arrange
        var guarda = new CrawlerExecutionGuard();

        // Act
        var adquiriu = guarda.TryAcquire();

        // Assert
        adquiriu.ShouldBeTrue();
        guarda.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public void Given_AposRelease_Should_NaoEstarExecutando()
    {
        // Arrange
        var guarda = new CrawlerExecutionGuard();
        guarda.TryAcquire();

        // Act
        guarda.Release();

        // Assert
        guarda.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Given_DuploTryAcquire_Should_RetornarFalsoNaSegundaChamada()
    {
        // Arrange
        var guarda = new CrawlerExecutionGuard();
        guarda.TryAcquire();

        // Act
        var segundaAquisicao = guarda.TryAcquire();

        // Assert
        segundaAquisicao.ShouldBeFalse();
        guarda.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public void Given_AcquireReleaseAcquire_Should_PermitirNovaAquisicao()
    {
        // Arrange
        var guarda = new CrawlerExecutionGuard();
        guarda.TryAcquire();
        guarda.Release();

        // Act
        var reAdquiriu = guarda.TryAcquire();

        // Assert
        reAdquiriu.ShouldBeTrue();
        guarda.IsRunning.ShouldBeTrue();
    }
}

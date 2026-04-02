using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using MapaTributario.API.Infrastructure.Seed;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ConfiguracaoCrawlerSeedServiceTests
{
    private readonly Mock<IConfiguracaoCrawlerRepository> _repository = new();
    private readonly Mock<ILogger<ConfiguracaoCrawlerSeedService>> _logger = new();

    [Fact]
    public async Task Given_NenhumaConfiguracaoExistente_Should_CriarConfiguracaoPadrao()
    {
        // Arrange
        _repository.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((ConfiguracaoCrawler?)null);
        _repository.Setup(r => r.CriarAsync(It.IsAny<ConfiguracaoCrawler>()))
            .ReturnsAsync((ConfiguracaoCrawler c) => c);

        var sut = new ConfiguracaoCrawlerSeedService(_repository.Object, _logger.Object);

        // Act
        await sut.SeedAsync();

        // Assert
        _repository.Verify(r => r.CriarAsync(It.Is<ConfiguracaoCrawler>(c =>
            c.Ativo == true &&
            c.TamanheLoteMongo == 50 &&
            c.MaxTentativas == 3 &&
            c.CronSchedule == "0 2 * * *"
        )), Times.Once);
    }

    [Fact]
    public async Task Given_ConfiguracaoJaExistente_Should_IgnorarSeed()
    {
        // Arrange
        ConfiguracaoCrawler existente = ConfiguracaoCrawler.CriarPadrao();
        existente.SetId("abc123");
        _repository.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(existente);

        var sut = new ConfiguracaoCrawlerSeedService(_repository.Object, _logger.Object);

        // Act
        await sut.SeedAsync();

        // Assert
        _repository.Verify(r => r.CriarAsync(It.IsAny<ConfiguracaoCrawler>()), Times.Never);
    }
}

using MapaTributario.API.Domain.Entities;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ConfiguracaoCrawlerTests
{
    [Fact]
    public void CriarPadrao_Deve_RetornarConfiguracaoComValoresPadrao()
    {
        // Arrange & Act
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();

        // Assert
        configuracao.CronSchedule.ShouldBe("0 2 * * *");
        configuracao.LimiteRequisicoesPorSegundo.ShouldBe(15);
        configuracao.OrcamentoDiario.ShouldBe(50000);
        configuracao.TamanheLoteCertificado.ShouldBe(200);
        configuracao.PausaLoteSegundos.ShouldBe(5);
        configuracao.TamanheLoteMongo.ShouldBe(50);
        configuracao.MaxTentativas.ShouldBe(3);
        configuracao.LimiteParadaAntecipada.ShouldBe(9);
        configuracao.MaxDesdobramento.ShouldBe(20);
        configuracao.MaxDetalhamento.ShouldBe(99);
        configuracao.MaxFalhasConsecutivasDetalhamento.ShouldBe(2);
        configuracao.MaxFalhasConsecutivasDesdobramento.ShouldBe(2);
        configuracao.MaxItensParalelos.ShouldBe(10);
        configuracao.ValidadeDiasProcessamento.ShouldBe(7);
        configuracao.CircuitBreakerLimiarErroPercent.ShouldBe(50);
        configuracao.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(60);
        configuracao.CircuitBreakerPausaSegundos.ShouldBe(300);
        configuracao.CircuitBreakerAmostraMinima.ShouldBe(10);
        configuracao.Ativo.ShouldBeTrue();
        configuracao.CriadoEm.ShouldNotBe(default);
        configuracao.AtualizadoEm.ShouldNotBe(default);
    }

    [Fact]
    public void CriarPadrao_Deve_RetornarCodigosSondagemCorretos()
    {
        // Arrange & Act
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();

        // Assert
        configuracao.CodigosSondagem.ShouldNotBeNull();
        configuracao.CodigosSondagem.Count.ShouldBe(5);
        configuracao.CodigosSondagem.ShouldContain("01.01.01");
        configuracao.CodigosSondagem.ShouldContain("07.02.01");
        configuracao.CodigosSondagem.ShouldContain("14.01.01");
        configuracao.CodigosSondagem.ShouldContain("17.01.01");
        configuracao.CodigosSondagem.ShouldContain("25.01.01");
    }

    [Fact]
    public void MarcarAtualizado_Deve_AtualizarTimestamp()
    {
        // Arrange
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();
        DateTime timestampOriginal = configuracao.AtualizadoEm;

        // Garantir diferença de tempo
        System.Threading.Thread.Sleep(10);

        // Act
        configuracao.MarcarAtualizado();

        // Assert
        configuracao.AtualizadoEm.ShouldBeGreaterThan(timestampOriginal);
    }

    [Fact]
    public void SetId_Deve_DefinirId()
    {
        // Arrange
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();

        // Act
        configuracao.SetId("abc123");

        // Assert
        configuracao.Id.ShouldBe("abc123");
    }
}

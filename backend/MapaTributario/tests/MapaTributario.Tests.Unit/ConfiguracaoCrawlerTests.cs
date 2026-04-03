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
        configuracao.LimiteRequisicoesPorSegundo.ShouldBe(50);
        configuracao.OrcamentoDiario.ShouldBe(200000);
        configuracao.TamanhoLoteCertificado.ShouldBe(500);
        configuracao.PausaLoteSegundos.ShouldBe(0);
        configuracao.TamanhoLoteMongo.ShouldBe(50);
        configuracao.MaxTentativas.ShouldBe(3);
        configuracao.LimiteParadaAntecipada.ShouldBe(9);
        configuracao.MaxDesdobramento.ShouldBe(20);
        configuracao.MaxDetalhamento.ShouldBe(99);
        configuracao.MaxFalhasConsecutivasDetalhamento.ShouldBe(2);
        configuracao.MaxFalhasConsecutivasDesdobramento.ShouldBe(2);
        configuracao.MaxItensParalelos.ShouldBe(20);
        configuracao.MaxUfsParalelas.ShouldBe(5);
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

        // Act
        configuracao.MarcarAtualizado();

        // Assert
        configuracao.AtualizadoEm.ShouldBeGreaterThanOrEqualTo(timestampOriginal);
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

    [Fact]
    public void Atualizar_Deve_AtualizarTodasPropriedades()
    {
        // Arrange
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();
        var novosCodigosSondagem = new List<string> { "01.01.01", "07.02.01" };

        // Act
        configuracao.Atualizar(
            cronSchedule: "0 5 * * *",
            limiteRequisicoesPorSegundo: 25,
            orcamentoDiario: 80000,
            tamanhoLoteCertificado: 400,
            pausaLoteSegundos: 15,
            tamanhoLoteMongo: 75,
            maxTentativas: 6,
            limiteParadaAntecipada: 15,
            maxDesdobramento: 40,
            maxDetalhamento: 120,
            maxFalhasConsecutivasDetalhamento: 5,
            maxFalhasConsecutivasDesdobramento: 5,
            maxItensParalelos: 20,
            maxUfsParalelas: 10,
            codigosSondagem: novosCodigosSondagem,
            validadeDiasProcessamento: 10,
            circuitBreakerLimiarErroPercent: 80,
            circuitBreakerJanelaAvaliacaoSegundos: 90,
            circuitBreakerPausaSegundos: 500,
            circuitBreakerAmostraMinima: 15,
            ativo: false);

        // Assert
        configuracao.CronSchedule.ShouldBe("0 5 * * *");
        configuracao.LimiteRequisicoesPorSegundo.ShouldBe(25);
        configuracao.OrcamentoDiario.ShouldBe(80000);
        configuracao.TamanhoLoteCertificado.ShouldBe(400);
        configuracao.PausaLoteSegundos.ShouldBe(15);
        configuracao.TamanhoLoteMongo.ShouldBe(75);
        configuracao.MaxTentativas.ShouldBe(6);
        configuracao.LimiteParadaAntecipada.ShouldBe(15);
        configuracao.MaxDesdobramento.ShouldBe(40);
        configuracao.MaxDetalhamento.ShouldBe(120);
        configuracao.MaxFalhasConsecutivasDetalhamento.ShouldBe(5);
        configuracao.MaxFalhasConsecutivasDesdobramento.ShouldBe(5);
        configuracao.MaxItensParalelos.ShouldBe(20);
        configuracao.MaxUfsParalelas.ShouldBe(10);
        configuracao.CodigosSondagem.ShouldBe(novosCodigosSondagem);
        configuracao.ValidadeDiasProcessamento.ShouldBe(10);
        configuracao.CircuitBreakerLimiarErroPercent.ShouldBe(80);
        configuracao.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(90);
        configuracao.CircuitBreakerPausaSegundos.ShouldBe(500);
        configuracao.CircuitBreakerAmostraMinima.ShouldBe(15);
        configuracao.Ativo.ShouldBeFalse();
    }

    [Fact]
    public void AtualizarParcial_Deve_AtualizarApenasCamposInformados()
    {
        // Arrange
        ConfiguracaoCrawler configuracao = ConfiguracaoCrawler.CriarPadrao();

        // Act
        configuracao.AtualizarParcial(
            cronSchedule: "0 4 * * *",
            maxTentativas: 7);

        // Assert — campos alterados
        configuracao.CronSchedule.ShouldBe("0 4 * * *");
        configuracao.MaxTentativas.ShouldBe(7);

        // Assert — campos inalterados (valores padrão)
        configuracao.LimiteRequisicoesPorSegundo.ShouldBe(50);
        configuracao.OrcamentoDiario.ShouldBe(200000);
        configuracao.TamanhoLoteCertificado.ShouldBe(500);
        configuracao.PausaLoteSegundos.ShouldBe(0);
        configuracao.TamanhoLoteMongo.ShouldBe(50);
        configuracao.LimiteParadaAntecipada.ShouldBe(9);
        configuracao.MaxDesdobramento.ShouldBe(20);
        configuracao.MaxDetalhamento.ShouldBe(99);
        configuracao.MaxFalhasConsecutivasDetalhamento.ShouldBe(2);
        configuracao.MaxFalhasConsecutivasDesdobramento.ShouldBe(2);
        configuracao.MaxItensParalelos.ShouldBe(20);
        configuracao.MaxUfsParalelas.ShouldBe(5);
        configuracao.ValidadeDiasProcessamento.ShouldBe(7);
        configuracao.CircuitBreakerLimiarErroPercent.ShouldBe(50);
        configuracao.CircuitBreakerJanelaAvaliacaoSegundos.ShouldBe(60);
        configuracao.CircuitBreakerPausaSegundos.ShouldBe(300);
        configuracao.CircuitBreakerAmostraMinima.ShouldBe(10);
        configuracao.Ativo.ShouldBeTrue();
    }
}

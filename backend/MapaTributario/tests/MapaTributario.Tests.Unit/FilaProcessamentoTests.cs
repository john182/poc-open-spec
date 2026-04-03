using MapaTributario.API.Domain.Entities;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class FilaProcessamentoTests
{
    [Fact]
    public void Create_RetornaEntidadeCorreta()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");

        item.CodigoMunicipio.ShouldBe("3106200");
        item.CodigoServico.ShouldBe("01.01.01");
        item.Competencia.ShouldBe("2026-04-01");
        item.ExecucaoId.ShouldBe("exec1");
        item.Status.ShouldBe(StatusFila.Pendente);
        item.Tentativas.ShouldBe(0);
        item.UltimoErro.ShouldBeNull();
        item.ProximaTentativa.ShouldBeNull();
    }

    [Fact]
    public void MarcarProcessando_AtualizaStatus()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        DateTime antes = item.AtualizadoEm;

        item.MarcarProcessando();

        item.Status.ShouldBe(StatusFila.Processando);
        item.AtualizadoEm.ShouldBeGreaterThanOrEqualTo(antes);
    }

    [Fact]
    public void MarcarConcluido_AtualizaStatus()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        item.MarcarProcessando();

        item.MarcarConcluido();

        item.Status.ShouldBe(StatusFila.Concluido);
    }

    [Fact]
    public void MarcarErro_PrimeiraTentativa_DefineProximaTentativa()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");

        item.MarcarErro("Timeout", 3);

        item.Status.ShouldBe(StatusFila.Erro);
        item.Tentativas.ShouldBe(1);
        item.UltimoErro.ShouldBe("Timeout");
        item.ProximaTentativa.ShouldNotBeNull();
    }

    [Fact]
    public void MarcarErro_SegundaTentativa_AumentaBackoff()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");

        item.MarcarErro("Timeout", 3);
        DateTime? primeiraTentativa = item.ProximaTentativa;

        item.MarcarErro("Timeout", 3);
        DateTime? segundaTentativa = item.ProximaTentativa;

        item.Tentativas.ShouldBe(2);
        segundaTentativa!.Value.ShouldBeGreaterThan(primeiraTentativa!.Value);
    }

    [Fact]
    public void MarcarErro_MaxTentativasAtingidas_SemProximaTentativa()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");

        item.MarcarErro("Erro", 3);
        item.MarcarErro("Erro", 3);
        item.MarcarErro("Erro", 3);

        item.Tentativas.ShouldBe(3);
        item.ProximaTentativa.ShouldBeNull();
        item.PodeRetentar(3).ShouldBeFalse();
    }

    [Fact]
    public void PodeRetentar_ComTentativasRestantes_RetornaTrue()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        item.MarcarErro("Timeout", 3);

        item.PodeRetentar(3).ShouldBeTrue();
    }

    [Fact]
    public void PodeRetentar_SemTentativasRestantes_RetornaFalse()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        item.MarcarErro("Erro", 3);
        item.MarcarErro("Erro", 3);
        item.MarcarErro("Erro", 3);

        item.PodeRetentar(3).ShouldBeFalse();
    }

    [Fact]
    public void ReverterParaPendente_AtualizaStatus()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        item.MarcarProcessando();

        item.ReverterParaPendente();

        item.Status.ShouldBe(StatusFila.Pendente);
    }

    [Fact]
    public void SetId_AtribuiId()
    {
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");
        item.SetId("abc123");
        item.Id.ShouldBe("abc123");
    }

    [Fact]
    public void MarcarErro_ComBackoff_CalculaCorretamente()
    {
        // baseDelay = 30, multiplier = 4
        // Tentativa 1: 30 * 4^0 = 30s
        // Tentativa 2: 30 * 4^1 = 120s
        // Tentativa 3: 30 * 4^2 = 480s
        FilaProcessamento item = FilaProcessamento.Create("3106200", "01.01.01", "2026-04-01", "exec1", "MG");

        DateTime antes = DateTime.UtcNow;
        item.MarcarErro("Erro", 3, baseDelaySeconds: 30, backoffMultiplier: 4);

        item.ProximaTentativa.ShouldNotBeNull();
        // First retry: 30 * 4^0 = 30s
        double seconds = (item.ProximaTentativa!.Value - antes).TotalSeconds;
        seconds.ShouldBeGreaterThanOrEqualTo(28); // accounting for timing variance
        seconds.ShouldBeLessThan(35);

        DateTime antes2 = DateTime.UtcNow;
        item.MarcarErro("Erro", 3, baseDelaySeconds: 30, backoffMultiplier: 4);

        // Second retry: 30 * 4^1 = 120s
        double seconds2 = (item.ProximaTentativa!.Value - antes2).TotalSeconds;
        seconds2.ShouldBeGreaterThanOrEqualTo(115);
        seconds2.ShouldBeLessThan(130);
    }
}

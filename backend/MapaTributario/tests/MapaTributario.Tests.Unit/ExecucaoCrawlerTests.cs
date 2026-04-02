using MapaTributario.API.Domain.Entities;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ExecucaoCrawlerTests
{
    [Fact]
    public void Create_ComTipoAgendado_RetornaEntidadeCorreta()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);

        execucao.Status.ShouldBe(StatusExecucao.EmAndamento);
        execucao.Tipo.ShouldBe(TipoExecucao.Agendado);
        execucao.TotalMunicipios.ShouldBe(0);
        execucao.TotalServicos.ShouldBe(0);
        execucao.Processados.ShouldBe(0);
        execucao.Erros.ShouldBe(0);
        execucao.DetalhesErro.ShouldBeEmpty();
        execucao.Fim.ShouldBeNull();
    }

    [Fact]
    public void Create_ComTipoManual_RetornaEntidadeCorreta()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.Tipo.ShouldBe(TipoExecucao.Manual);
        execucao.Status.ShouldBe(StatusExecucao.EmAndamento);
    }

    [Fact]
    public void SetTotais_AtualizaValores()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.SetTotais(27, 600);

        execucao.TotalMunicipios.ShouldBe(27);
        execucao.TotalServicos.ShouldBe(600);
    }

    [Fact]
    public void IncrementarProcessados_IncrementaContador()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IncrementarProcessados();
        execucao.IncrementarProcessados();

        execucao.Processados.ShouldBe(2);
    }

    [Fact]
    public void IncrementarErros_IncrementaContadorEAdicionaDetalhe()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IncrementarErros("Timeout apos 30s");
        execucao.IncrementarErros("Erro 500");

        execucao.Erros.ShouldBe(2);
        execucao.DetalhesErro.Count.ShouldBe(2);
        execucao.DetalhesErro[0].ShouldBe("Timeout apos 30s");
    }

    [Fact]
    public void Finalizar_AtualizaStatusEFim()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.Finalizar(StatusExecucao.Concluido);

        execucao.Status.ShouldBe(StatusExecucao.Concluido);
        execucao.Fim.ShouldNotBeNull();
    }

    [Fact]
    public void Finalizar_ComFalhaParcial()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IncrementarErros("Erro");

        execucao.Finalizar(StatusExecucao.FalhaParcial);

        execucao.Status.ShouldBe(StatusExecucao.FalhaParcial);
    }

    [Fact]
    public void SetId_AtribuiId()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.SetId("abc123");
        execucao.Id.ShouldBe("abc123");
    }

    [Fact]
    public void FinalizarProcessamentoUf_AtualizaStatusMunicipiosEncontradosEAtivos()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IniciarProcessamentoUf("SP");

        execucao.FinalizarProcessamentoUf("SP", municipiosEncontrados: 100, municipiosAtivos: 42);

        execucao.ProgressoUfs["SP"].Status.ShouldBe("Concluido");
        execucao.ProgressoUfs["SP"].MunicipiosEncontrados.ShouldBe(100);
        execucao.ProgressoUfs["SP"].MunicipiosAtivos.ShouldBe(42);
        execucao.ProgressoUfs["SP"].Fim.ShouldNotBeNull();
        execucao.UfAtual.ShouldBeNull();
    }

    [Fact]
    public void FalharProcessamentoUf_MarcaStatusFalhaComMunicipiosAtivosZero()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IniciarProcessamentoUf("RJ");

        execucao.FalharProcessamentoUf("RJ", municipiosEncontrados: 50);

        execucao.ProgressoUfs["RJ"].Status.ShouldBe("Falha");
        execucao.ProgressoUfs["RJ"].MunicipiosEncontrados.ShouldBe(50);
        execucao.ProgressoUfs["RJ"].MunicipiosAtivos.ShouldBe(0);
        execucao.ProgressoUfs["RJ"].Fim.ShouldNotBeNull();
        execucao.UfAtual.ShouldBeNull();
    }

    [Fact]
    public void InterromperProcessamentoUf_MarcaStatusInterrompidoComAtivosAteAgora()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IniciarProcessamentoUf("MG");

        execucao.InterromperProcessamentoUf("MG", municipiosEncontrados: 80, municipiosAtivosAteAgora: 15);

        execucao.ProgressoUfs["MG"].Status.ShouldBe("Interrompido");
        execucao.ProgressoUfs["MG"].MunicipiosEncontrados.ShouldBe(80);
        execucao.ProgressoUfs["MG"].MunicipiosAtivos.ShouldBe(15);
        execucao.ProgressoUfs["MG"].Fim.ShouldNotBeNull();
        execucao.UfAtual.ShouldBeNull();
    }

    [Fact]
    public void ProgressoUf_StatusInicial_DeveSePendente()
    {
        ProgressoUf progresso = new();

        progresso.Status.ShouldBe("Pendente");
        progresso.MunicipiosEncontrados.ShouldBe(0);
        progresso.MunicipiosAtivos.ShouldBe(0);
    }

    [Fact]
    public void IniciarProcessamentoUf_CriaProgressoComStatusEmAndamento()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IniciarProcessamentoUf("SE");

        execucao.ProgressoUfs.ShouldContainKey("SE");
        execucao.ProgressoUfs["SE"].Status.ShouldBe("EmAndamento");
        execucao.ProgressoUfs["SE"].Inicio.ShouldNotBeNull();
        execucao.UfAtual.ShouldBe("SE");
    }
}

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

        execucao.ProgressoUfs["SP"].Status.ShouldBe(StatusProgressoUf.Concluido);
        execucao.ProgressoUfs["SP"].MunicipiosEncontrados.ShouldBe(100);
        execucao.ProgressoUfs["SP"].MunicipiosAtivos.ShouldBe(42);
        execucao.ProgressoUfs["SP"].Fim.ShouldNotBeNull();
        execucao.UfsEmAndamento.ShouldBeEmpty();
    }

    [Fact]
    public void FalharProcessamentoUf_MarcaStatusFalhaComMunicipiosAtivosZero()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IniciarProcessamentoUf("RJ");

        execucao.FalharProcessamentoUf("RJ", municipiosEncontrados: 50);

        execucao.ProgressoUfs["RJ"].Status.ShouldBe(StatusProgressoUf.Falha);
        execucao.ProgressoUfs["RJ"].MunicipiosEncontrados.ShouldBe(50);
        execucao.ProgressoUfs["RJ"].MunicipiosAtivos.ShouldBe(0);
        execucao.ProgressoUfs["RJ"].Fim.ShouldNotBeNull();
        execucao.UfsEmAndamento.ShouldBeEmpty();
    }

    [Fact]
    public void InterromperProcessamentoUf_MarcaStatusInterrompidoComAtivosAteAgora()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.IniciarProcessamentoUf("MG");

        execucao.InterromperProcessamentoUf("MG", municipiosEncontrados: 80, municipiosAtivosAteAgora: 15);

        execucao.ProgressoUfs["MG"].Status.ShouldBe(StatusProgressoUf.Interrompido);
        execucao.ProgressoUfs["MG"].MunicipiosEncontrados.ShouldBe(80);
        execucao.ProgressoUfs["MG"].MunicipiosAtivos.ShouldBe(15);
        execucao.ProgressoUfs["MG"].Fim.ShouldNotBeNull();
        execucao.UfsEmAndamento.ShouldBeEmpty();
    }

    [Fact]
    public void ProgressoUf_StatusInicial_DeveSerPendente()
    {
        ProgressoUf progresso = new();

        progresso.Status.ShouldBe(StatusProgressoUf.Pendente);
        progresso.MunicipiosEncontrados.ShouldBe(0);
        progresso.MunicipiosAtivos.ShouldBe(0);
    }

    [Fact]
    public void IniciarProcessamentoUf_CriaProgressoComStatusEmAndamento()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IniciarProcessamentoUf("SE");

        execucao.ProgressoUfs.ShouldContainKey("SE");
        execucao.ProgressoUfs["SE"].Status.ShouldBe(StatusProgressoUf.EmAndamento);
        execucao.ProgressoUfs["SE"].Inicio.ShouldNotBeNull();
        execucao.UfsEmAndamento.ShouldContain("SE");
    }

    [Fact]
    public void IniciarProcessamentoUf_MultiplasUfs_TodasApareceEmAndamento()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IniciarProcessamentoUf("SP");
        execucao.IniciarProcessamentoUf("RJ");
        execucao.IniciarProcessamentoUf("MG");

        execucao.UfsEmAndamento.Count.ShouldBe(3);
        execucao.UfsEmAndamento.ShouldContain("SP");
        execucao.UfsEmAndamento.ShouldContain("RJ");
        execucao.UfsEmAndamento.ShouldContain("MG");
        execucao.ProgressoUfs.Count.ShouldBe(3);
    }

    [Fact]
    public void FinalizarProcessamentoUf_RemoveDeUfsEmAndamento()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IniciarProcessamentoUf("SP");
        execucao.IniciarProcessamentoUf("RJ");
        execucao.FinalizarProcessamentoUf("SP", municipiosEncontrados: 645, municipiosAtivos: 200);

        execucao.UfsEmAndamento.Count.ShouldBe(1);
        execucao.UfsEmAndamento.ShouldContain("RJ");
        execucao.UfsEmAndamento.ShouldNotContain("SP");
    }

    [Fact]
    public async Task ThreadSafety_MultiplasThreadsIniciarEFinalizar_SemExcecao()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        string[] ufs = new[] { "SP", "RJ", "MG", "RS", "BA", "PR", "SC", "PE", "CE", "GO" };

        // Múltiplas threads iniciando UFs simultaneamente
        Task[] tarefasIniciar = ufs.Select(uf => Task.Run(() =>
        {
            execucao.IniciarProcessamentoUf(uf);
        })).ToArray();

        await Task.WhenAll(tarefasIniciar);

        execucao.ProgressoUfs.Count.ShouldBe(10);
        execucao.UfsEmAndamento.Count.ShouldBe(10);

        // Múltiplas threads finalizando UFs simultaneamente
        Task[] tarefasFinalizar = ufs.Select(uf => Task.Run(() =>
        {
            execucao.FinalizarProcessamentoUf(uf, municipiosEncontrados: 100, municipiosAtivos: 50);
        })).ToArray();

        await Task.WhenAll(tarefasFinalizar);

        execucao.UfsEmAndamento.ShouldBeEmpty();
        foreach (string uf in ufs)
        {
            execucao.ProgressoUfs[uf].Status.ShouldBe(StatusProgressoUf.Concluido);
        }
    }

    [Fact]
    public async Task ThreadSafety_IniciarEFinalizarIntercalados_SemExcecao()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        string[] ufs = new[] { "SP", "RJ", "MG", "RS", "BA" };

        // Threads intercalando iniciar e finalizar
        List<Task> tarefas = new();
        foreach (string uf in ufs)
        {
            tarefas.Add(Task.Run(async () =>
            {
                execucao.IniciarProcessamentoUf(uf);
                await Task.Delay(Random.Shared.Next(1, 10));
                execucao.FinalizarProcessamentoUf(uf, municipiosEncontrados: 50, municipiosAtivos: 20);
            }));
        }

        await Task.WhenAll(tarefas);

        execucao.UfsEmAndamento.ShouldBeEmpty();
        execucao.ProgressoUfs.Count.ShouldBe(5);
        foreach (string uf in ufs)
        {
            execucao.ProgressoUfs[uf].Status.ShouldBe(StatusProgressoUf.Concluido);
        }
    }

    [Fact]
    public async Task ThreadSafety_FalharEInterromperSimultaneo_SemExcecao()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        // Iniciar todas as UFs
        string[] ufsParaFalhar = new[] { "SP", "RJ", "MG" };
        string[] ufsParaInterromper = new[] { "BA", "CE", "PE" };
        string[] todasUfs = ufsParaFalhar.Concat(ufsParaInterromper).ToArray();

        foreach (string uf in todasUfs)
        {
            execucao.IniciarProcessamentoUf(uf);
        }

        execucao.UfsEmAndamento.Count.ShouldBe(6);

        // Falhar e interromper simultaneamente
        Task[] tarefas = ufsParaFalhar.Select(uf => Task.Run(() =>
        {
            execucao.FalharProcessamentoUf(uf, municipiosEncontrados: 100);
        }))
        .Concat(ufsParaInterromper.Select(uf => Task.Run(() =>
        {
            execucao.InterromperProcessamentoUf(uf, municipiosEncontrados: 80, municipiosAtivosAteAgora: 30);
        })))
        .ToArray();

        await Task.WhenAll(tarefas);

        execucao.UfsEmAndamento.ShouldBeEmpty();
        foreach (string uf in ufsParaFalhar)
        {
            execucao.ProgressoUfs[uf].Status.ShouldBe(StatusProgressoUf.Falha);
        }
        foreach (string uf in ufsParaInterromper)
        {
            execucao.ProgressoUfs[uf].Status.ShouldBe(StatusProgressoUf.Interrompido);
        }
    }

    [Fact]
    public void IniciarProcessamentoUf_MesmaUfDuasVezes_NaoDuplicaEmAndamento()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.IniciarProcessamentoUf("SP");
        execucao.IniciarProcessamentoUf("SP");

        execucao.UfsEmAndamento.Count.ShouldBe(1);
        execucao.ProgressoUfs.Count.ShouldBe(1);
    }

    #region FaseCrawler Tests

    [Fact]
    public void Dado_ExecucaoCriada_FaseAtualDeveSerDescobertaConvenios()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);

        execucao.FaseAtual.ShouldBe(FaseCrawler.DescobertaConvenios);
    }

    [Fact]
    public void Dado_AvancarFase_DeveAtualizarFaseAtual()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.AvancarFase(FaseCrawler.Sondagem);
        execucao.FaseAtual.ShouldBe(FaseCrawler.Sondagem);

        execucao.AvancarFase(FaseCrawler.ProcessamentoFila);
        execucao.FaseAtual.ShouldBe(FaseCrawler.ProcessamentoFila);

        execucao.AvancarFase(FaseCrawler.Concluido);
        execucao.FaseAtual.ShouldBe(FaseCrawler.Concluido);
    }

    [Fact]
    public void Dado_AvancarFase_ParaDescobertaConvenios_DevePermitirTransicao()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.AvancarFase(FaseCrawler.ProcessamentoFila);
        execucao.AvancarFase(FaseCrawler.DescobertaConvenios);

        execucao.FaseAtual.ShouldBe(FaseCrawler.DescobertaConvenios);
    }

    [Fact]
    public void Dado_Finalizar_FaseAtualDeveSerConcluido()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.AvancarFase(FaseCrawler.ProcessamentoFila);
        execucao.Finalizar(StatusExecucao.Concluido);

        execucao.FaseAtual.ShouldBe(FaseCrawler.Concluido);
        execucao.Status.ShouldBe(StatusExecucao.Concluido);
        execucao.Fim.ShouldNotBeNull();
    }

    [Fact]
    public void Dado_FinalizarComFalha_FaseAtualDeveSerConcluido()
    {
        ExecucaoCrawler execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);

        execucao.AvancarFase(FaseCrawler.Sondagem);
        execucao.Finalizar(StatusExecucao.Falha);

        execucao.FaseAtual.ShouldBe(FaseCrawler.Concluido);
        execucao.Status.ShouldBe(StatusExecucao.Falha);
    }

    #endregion
}

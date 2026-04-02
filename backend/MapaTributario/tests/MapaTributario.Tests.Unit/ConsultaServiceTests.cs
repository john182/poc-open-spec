using MapaTributario.API.Application.Consulta;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ConsultaServiceTests
{
    private readonly Mock<IEstadoRepository> _estadoRepository = new();
    private readonly Mock<IMunicipioRepository> _municipioRepository = new();
    private readonly Mock<IAliquotaRepository> _aliquotaRepository = new();
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IExecucaoCrawlerRepository> _execucaoCrawlerRepository = new();
    private readonly Mock<ICrawlerExecutionGuard> _executionGuard = new();
    private readonly Mock<ICertificadoStore> _certificadoStore = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<ILogger<ConsultaService>> _logger = new();
    private readonly ConsultaService _sut;

    public ConsultaServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Crawler:ValidadeDiasProcessamento", "7" }
            })
            .Build();

        // Setup padrão: guard não está rodando (permite disparo)
        _executionGuard.Setup(g => g.IsRunning).Returns(false);

        // Setup padrão: certificado disponível
        _certificadoStore.Setup(s => s.HasCertificate()).Returns(true);

        // Setup padrão: serviço repository retorna dicionário vazio (sem enriquecimento)
        _servicoRepository.Setup(r => r.ObterDescricoesPorCodigosAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, string>() as IReadOnlyDictionary<string, string>);
        _servicoRepository.Setup(r => r.GetByCodigoAsync(It.IsAny<string>()))
            .ReturnsAsync((Servico?)null);

        // Setup scope factory para fire-and-forget (não será realmente executado nos testes unitários)
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockCrawlerService = new Mock<ICrawlerService>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(ICrawlerService)))
            .Returns(mockCrawlerService.Object);
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        _sut = new ConsultaService(
            _estadoRepository.Object,
            _municipioRepository.Object,
            _aliquotaRepository.Object,
            _servicoRepository.Object,
            _execucaoCrawlerRepository.Object,
            _executionGuard.Object,
            _certificadoStore.Object,
            _scopeFactory.Object,
            _logger.Object,
            configuration);
    }

    // --- ListarEstados ---

    [Fact]
    public async Task ListarEstados_RetornaEstadosOrdenados()
    {
        var estados = new List<Estado>
        {
            Estado.Create("MG", "Minas Gerais", "SE"),
            Estado.Create("SP", "São Paulo", "SE"),
            Estado.Create("RJ", "Rio de Janeiro", "SE")
        };
        _estadoRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(estados);

        var result = await _sut.ListarEstadosAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3);
        result.Value[0].Sigla.ShouldBe("MG");
        result.Value[0].Nome.ShouldBe("Minas Gerais");
        result.Value[0].Regiao.ShouldBe("SE");
    }

    [Fact]
    public async Task ListarEstados_SemEstados_RetornaListaVazia()
    {
        _estadoRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Estado>());

        var result = await _sut.ListarEstadosAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(0);
    }

    // --- ListarMunicipiosPorUf ---

    [Fact]
    public async Task ListarMunicipiosPorUf_UfValida_Concluido_RetornaMunicipiosComAliquotas()
    {
        var estado = Estado.Create("SP", "São Paulo", "SE");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("SP")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);
        execucao.SetUfsProcessadas(new[] { "SP" });
        execucao.Finalizar(StatusExecucao.Concluido);
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("SP")).ReturnsAsync(execucao);

        var municipios = new List<Municipio>
        {
            Municipio.Create("3550308", "São Paulo", "SP"),
            Municipio.Create("3509502", "Campinas", "SP"),
            Municipio.Create("3547809", "Santos", "SP")
        };
        _municipioRepository.Setup(r => r.GetByUfAsync("SP")).ReturnsAsync(municipios);

        var codigosComAliquota = new HashSet<string> { "3550308", "3509502" } as IReadOnlySet<string>;
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(codigosComAliquota);

        var result = await _sut.ListarMunicipiosPorUfAsync("SP");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Concluido);
        result.Value.Municipios.Count.ShouldBe(2);
        result.Value.Municipios[0].CodigoIbge.ShouldBe("3550308");
        result.Value.Municipios[0].Nome.ShouldBe("São Paulo");
        result.Value.Municipios[0].SiglaEstado.ShouldBe("SP");
        result.Value.Municipios[0].PossuiAliquotas.ShouldBeTrue();
        result.Value.Municipios[1].CodigoIbge.ShouldBe("3509502");
        result.Value.Municipios.ShouldNotContain(m => m.CodigoIbge == "3547809");
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_UfInvalida_RetornaNotFoundError()
    {
        _estadoRepository.Setup(r => r.GetBySiglaAsync("XX")).ReturnsAsync((Estado?)null);

        var result = await _sut.ListarMunicipiosPorUfAsync("XX");

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("XX");
        result.Errors.First().Message.ShouldContain("não encontrada");
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_NuncaProcessado_GuardLivre_RetornaProcessamentoIniciado()
    {
        var estado = Estado.Create("DF", "Distrito Federal", "CO");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("DF")).ReturnsAsync(estado);
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("DF")).ReturnsAsync((ExecucaoCrawler?)null);
        _executionGuard.Setup(g => g.IsRunning).Returns(false);

        var result = await _sut.ListarMunicipiosPorUfAsync("DF");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.ProcessamentoIniciado);
        result.Value.Municipios.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_NuncaProcessado_GuardOcupado_RetornaAguardandoProcessamento()
    {
        var estado = Estado.Create("DF", "Distrito Federal", "CO");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("DF")).ReturnsAsync(estado);
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("DF")).ReturnsAsync((ExecucaoCrawler?)null);
        _executionGuard.Setup(g => g.IsRunning).Returns(true);

        var result = await _sut.ListarMunicipiosPorUfAsync("DF");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.AguardandoProcessamento);
        result.Value.Municipios.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_EmAndamento_RetornaProcessandoComDadosParciais()
    {
        var estado = Estado.Create("RJ", "Rio de Janeiro", "SE");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("RJ")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.SetUfsProcessadas(new[] { "RJ" });
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("RJ")).ReturnsAsync(execucao);

        var municipios = new List<Municipio>
        {
            Municipio.Create("3304557", "Rio de Janeiro", "RJ")
        };
        _municipioRepository.Setup(r => r.GetByUfAsync("RJ")).ReturnsAsync(municipios);

        var codigosComAliquota = new HashSet<string> { "3304557" } as IReadOnlySet<string>;
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(codigosComAliquota);

        var result = await _sut.ListarMunicipiosPorUfAsync("RJ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Processando);
        result.Value.Municipios.Count.ShouldBe(1);
        result.Value.Municipios[0].CodigoIbge.ShouldBe("3304557");
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_EmAndamento_SemDados_RetornaProcessandoListaVazia()
    {
        var estado = Estado.Create("RJ", "Rio de Janeiro", "SE");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("RJ")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Manual);
        execucao.SetUfsProcessadas(new[] { "RJ" });
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("RJ")).ReturnsAsync(execucao);

        _municipioRepository.Setup(r => r.GetByUfAsync("RJ")).ReturnsAsync(new List<Municipio>());
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(new HashSet<string>() as IReadOnlySet<string>);

        var result = await _sut.ListarMunicipiosPorUfAsync("RJ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Processando);
        result.Value.Municipios.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_Vencido_GuardLivre_RetornaAtualizandoComDadosAntigos()
    {
        var estado = Estado.Create("SP", "São Paulo", "SE");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("SP")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);
        execucao.SetUfsProcessadas(new[] { "SP" });
        execucao.Finalizar(StatusExecucao.Concluido);
        // Simular execução vencida: Fim foi definido internamente pelo Finalizar como DateTime.UtcNow
        // Para testar vencido, precisamos de uma execução com Fim há mais de 7 dias
        // Como Finalizar usa DateTime.UtcNow, vamos usar reflection para forçar Fim antigo
        typeof(ExecucaoCrawler).GetProperty("Fim")!
            .SetValue(execucao, DateTime.UtcNow.AddDays(-10));
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("SP")).ReturnsAsync(execucao);

        var municipios = new List<Municipio>
        {
            Municipio.Create("3550308", "São Paulo", "SP")
        };
        _municipioRepository.Setup(r => r.GetByUfAsync("SP")).ReturnsAsync(municipios);

        var codigosComAliquota = new HashSet<string> { "3550308" } as IReadOnlySet<string>;
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(codigosComAliquota);

        _executionGuard.Setup(g => g.IsRunning).Returns(false);

        var result = await _sut.ListarMunicipiosPorUfAsync("SP");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Atualizando);
        result.Value.Municipios.Count.ShouldBe(1);
        result.Value.UltimoProcessamento.ShouldNotBeNull();
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_Vencido_GuardOcupado_RetornaVencidoComDadosAntigos()
    {
        var estado = Estado.Create("SP", "São Paulo", "SE");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("SP")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);
        execucao.SetUfsProcessadas(new[] { "SP" });
        execucao.Finalizar(StatusExecucao.Concluido);
        typeof(ExecucaoCrawler).GetProperty("Fim")!
            .SetValue(execucao, DateTime.UtcNow.AddDays(-10));
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("SP")).ReturnsAsync(execucao);

        var municipios = new List<Municipio>
        {
            Municipio.Create("3550308", "São Paulo", "SP")
        };
        _municipioRepository.Setup(r => r.GetByUfAsync("SP")).ReturnsAsync(municipios);

        var codigosComAliquota = new HashSet<string> { "3550308" } as IReadOnlySet<string>;
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(codigosComAliquota);

        _executionGuard.Setup(g => g.IsRunning).Returns(true);

        var result = await _sut.ListarMunicipiosPorUfAsync("SP");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Vencido);
        result.Value.Municipios.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListarMunicipiosPorUf_ConcluidoSemMunicipiosComAliquota_RetornaListaVazia()
    {
        var estado = Estado.Create("AC", "Acre", "N");
        _estadoRepository.Setup(r => r.GetBySiglaAsync("AC")).ReturnsAsync(estado);

        var execucao = ExecucaoCrawler.Create(TipoExecucao.Agendado);
        execucao.SetUfsProcessadas(new[] { "AC" });
        execucao.Finalizar(StatusExecucao.Concluido);
        _execucaoCrawlerRepository.Setup(r => r.GetLatestByUfAsync("AC")).ReturnsAsync(execucao);

        _municipioRepository.Setup(r => r.GetByUfAsync("AC")).ReturnsAsync(new List<Municipio>
        {
            Municipio.Create("1200401", "Rio Branco", "AC")
        });
        _aliquotaRepository.Setup(r => r.ListarCodigosMunicipiosComAliquotaAsync(
            It.IsAny<IEnumerable<string>>())).ReturnsAsync(new HashSet<string>() as IReadOnlySet<string>);

        var result = await _sut.ListarMunicipiosPorUfAsync("AC");

        result.IsSuccess.ShouldBeTrue();
        result.Value.StatusProcessamento.ShouldBe(StatusProcessamentoUf.Concluido);
        result.Value.Municipios.Count.ShouldBe(0);
    }

    // --- ListarAliquotasPorMunicipio ---

    [Fact]
    public async Task ListarAliquotasPorMunicipio_MunicipioValido_RetornaPaginado()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise e desenvolvimento", 2.0m, "2024-01", "NFS-e"),
            Aliquota.Create("3550308", "São Paulo", "010200", "01.02.00", "Programação", 3.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 1, 20, null, null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 2L));

        var queryParams = new AliquotaQueryParams { Pagina = 1, TamanhoPagina = 20 };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Pagina.ShouldBe(1);
        result.Value.TamanhoPagina.ShouldBe(20);
        result.Value.TotalItens.ShouldBe(2);
        result.Value.TotalPaginas.ShouldBe(1);
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_MunicipioInexistente_RetornaNotFoundError()
    {
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("9999999")).ReturnsAsync((Municipio?)null);

        var queryParams = new AliquotaQueryParams();
        var result = await _sut.ListarAliquotasPorMunicipioAsync("9999999", queryParams);

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("não encontrado");
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_ComFiltroCodigoServico_NormalizaCodigo()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise e desenvolvimento", 2.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 1, 20, "010100", null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 1L));

        var queryParams = new AliquotaQueryParams { CodigoServico = "01.01.00" };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_CodigoServicoInvalido_RetornaValidationError()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var queryParams = new AliquotaQueryParams { CodigoServico = "abc" };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<ValidationError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("formato inválido");
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_ComFiltroPrefixoCodigoServico_NormalizaPrefixo()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise e desenvolvimento", 2.0m, "2024-01", "NFS-e"),
            Aliquota.Create("3550308", "São Paulo", "010200", "01.02.00", "Programação", 3.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 1, 20, "01", null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 2L));

        var queryParams = new AliquotaQueryParams { CodigoServico = "01" };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_ComFiltroPrefixo4Digitos_NormalizaPrefixo()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise e desenvolvimento", 2.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 1, 20, "0101", null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 1L));

        var queryParams = new AliquotaQueryParams { CodigoServico = "01.01" };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_Paginacao_CalculaTotalPaginas()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise", 2.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 2, 5, null, null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 12L));

        var queryParams = new AliquotaQueryParams { Pagina = 2, TamanhoPagina = 5 };
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalPaginas.ShouldBe(3);
        result.Value.Pagina.ShouldBe(2);
    }

    [Fact]
    public async Task ListarAliquotasPorMunicipio_FormataCodigo_NaResposta()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquotas = new List<Aliquota>
        {
            Aliquota.Create("3550308", "São Paulo", "010200", "01.02.00", "Programação", 3.0m, "2024-01", "NFS-e")
        };
        _aliquotaRepository.Setup(r => r.GetByMunicipioAsync(
            "3550308", 1, 20, null, null, null, null, null))
            .ReturnsAsync((aliquotas as IReadOnlyList<Aliquota>, 1L));

        var queryParams = new AliquotaQueryParams();
        var result = await _sut.ListarAliquotasPorMunicipioAsync("3550308", queryParams);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items[0].CodigoServicoFormatado.ShouldBe("01.02.00");
    }

    // --- ObterDetalheAliquota ---

    [Fact]
    public async Task ObterDetalheAliquota_Existente_RetornaDetalhe()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquota = Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise", 2.0m, "2024-01", "NFS-e");
        _aliquotaRepository.Setup(r => r.GetDetalheAsync("3550308", "010100")).ReturnsAsync(aliquota);

        var result = await _sut.ObterDetalheAliquotaAsync("3550308", "01.01.00");

        result.IsSuccess.ShouldBeTrue();
        result.Value.CodigoMunicipio.ShouldBe("3550308");
        result.Value.NomeMunicipio.ShouldBe("São Paulo");
        result.Value.CodigoServico.ShouldBe("010100");
        result.Value.CodigoServicoFormatado.ShouldBe("01.01.00");
        result.Value.Aliquota.ShouldBe(2.0m);
    }

    [Fact]
    public async Task ObterDetalheAliquota_MunicipioInexistente_RetornaNotFoundError()
    {
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("9999999")).ReturnsAsync((Municipio?)null);

        var result = await _sut.ObterDetalheAliquotaAsync("9999999", "01.01.00");

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("não encontrado");
    }

    [Fact]
    public async Task ObterDetalheAliquota_AliquotaInexistente_RetornaNotFoundError()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);
        _aliquotaRepository.Setup(r => r.GetDetalheAsync("3550308", "990000")).ReturnsAsync((Aliquota?)null);

        var result = await _sut.ObterDetalheAliquotaAsync("3550308", "99.00.00");

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<NotFoundError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("não encontrada");
    }

    [Fact]
    public async Task ObterDetalheAliquota_CodigoServicoInvalido_RetornaValidationError()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var result = await _sut.ObterDetalheAliquotaAsync("3550308", "abc");

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<ValidationError>().ShouldNotBeEmpty();
        result.Errors.First().Message.ShouldContain("formato inválido");
    }

    [Fact]
    public async Task ObterDetalheAliquota_CodigoSemPontos_Funciona()
    {
        var municipio = Municipio.Create("3550308", "São Paulo", "SP");
        _municipioRepository.Setup(r => r.GetByCodigoIbgeAsync("3550308")).ReturnsAsync(municipio);

        var aliquota = Aliquota.Create("3550308", "São Paulo", "010100", "01.01.00", "Análise", 2.0m, "2024-01", "NFS-e");
        _aliquotaRepository.Setup(r => r.GetDetalheAsync("3550308", "010100")).ReturnsAsync(aliquota);

        var result = await _sut.ObterDetalheAliquotaAsync("3550308", "010100");

        result.IsSuccess.ShouldBeTrue();
        result.Value.CodigoServico.ShouldBe("010100");
    }
}

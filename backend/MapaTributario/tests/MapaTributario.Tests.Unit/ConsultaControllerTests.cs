#pragma warning disable CS8604 // ToString() em objetos anônimos retorna string? mas nunca é null nos testes
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using MapaTributario.API.Application.Consulta;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace MapaTributario.Tests.Unit;

public class ConsultaControllerTests
{
    private readonly Mock<IConsultaService> _consultaService = new();
    private readonly Mock<IValidator<AliquotaQueryParams>> _aliquotaValidator = new();

    private ConsultaController CriarSut()
    {
        return new ConsultaController(_consultaService.Object);
    }

    private void ConfigurarValidacaoComSucesso()
    {
        _aliquotaValidator.Setup(v => v.ValidateAsync(It.IsAny<AliquotaQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private void ConfigurarValidacaoComFalha(string mensagemErro = "Campo inválido")
    {
        ValidationResult resultado = new(new[]
        {
            new ValidationFailure("Campo", mensagemErro)
        });
        _aliquotaValidator.Setup(v => v.ValidateAsync(It.IsAny<AliquotaQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);
    }

    // ── ListarEstados ───────────────────────────────────────────────

    [Fact]
    public async Task Given_RequisicaoListarEstados_Should_RetornarOkComLista()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        List<EstadoResponse> estados = new()
        {
            new EstadoResponse { Sigla = "SP", Nome = "São Paulo", Regiao = "Sudeste" },
            new EstadoResponse { Sigla = "RJ", Nome = "Rio de Janeiro", Regiao = "Sudeste" }
        };
        _consultaService.Setup(s => s.ListarEstadosAsync())
            .ReturnsAsync(Result.Ok<IReadOnlyList<EstadoResponse>>(estados));

        // Act
        IActionResult resultado = await sut.ListarEstados();

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        IReadOnlyList<EstadoResponse> lista = ok.Value.ShouldBeAssignableTo<IReadOnlyList<EstadoResponse>>()!;
        lista.Count.ShouldBe(2);
        lista[0].Sigla.ShouldBe("SP");
    }

    // ── ListarMunicipiosPorUf ───────────────────────────────────────

    [Fact]
    public async Task Given_UfValida_Should_RetornarOkComMunicipios()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        MunicipiosUfResponse resposta = new()
        {
            StatusProcessamento = StatusProcessamentoUf.Concluido,
            UltimoProcessamento = DateTime.UtcNow,
            Municipios = new List<MunicipioResponse>
            {
                new() { CodigoIbge = "3550308", Nome = "São Paulo", SiglaEstado = "SP", PossuiAliquotas = true }
            }
        };
        _consultaService.Setup(s => s.ListarMunicipiosPorUfAsync("SP"))
            .ReturnsAsync(Result.Ok(resposta));

        // Act
        IActionResult resultado = await sut.ListarMunicipiosPorUf("SP");

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        MunicipiosUfResponse valor = ok.Value.ShouldBeOfType<MunicipiosUfResponse>();
        valor.Municipios.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Given_UfInexistente_Should_RetornarNotFound()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        _consultaService.Setup(s => s.ListarMunicipiosPorUfAsync("XX"))
            .ReturnsAsync(Result.Fail<MunicipiosUfResponse>(new NotFoundError("UF 'XX' não encontrada")));

        // Act
        IActionResult resultado = await sut.ListarMunicipiosPorUf("XX");

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("UF 'XX' não encontrada");
    }

    // ── ListarAliquotasPorMunicipio ─────────────────────────────────

    [Fact]
    public async Task Given_AliquotasComValidacaoFalha_Should_RetornarBadRequest()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        ConfigurarValidacaoComFalha("Página deve ser maior que zero");
        AliquotaQueryParams queryParams = new() { Pagina = 0 };

        // Act
        IActionResult resultado = await sut.ListarAliquotasPorMunicipio("3550308", queryParams, _aliquotaValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Validação falhou");
    }

    [Fact]
    public async Task Given_AliquotasComSucesso_Should_RetornarOkPaginado()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        ConfigurarValidacaoComSucesso();
        AliquotaQueryParams queryParams = new() { Pagina = 1, TamanhoPagina = 10 };
        List<AliquotaResponse> itens = new()
        {
            new AliquotaResponse
            {
                CodigoServico = "010101",
                CodigoServicoFormatado = "01.01.01",
                DescricaoServico = "Serviço de teste",
                Aliquota = 5.0m,
                Competencia = "2025-01"
            }
        };
        PaginatedResponse<AliquotaResponse> paginado = PaginatedResponse<AliquotaResponse>.Create(itens, 1, 10, 1);
        _consultaService.Setup(s => s.ListarAliquotasPorMunicipioAsync("3550308", queryParams))
            .ReturnsAsync(Result.Ok(paginado));

        // Act
        IActionResult resultado = await sut.ListarAliquotasPorMunicipio("3550308", queryParams, _aliquotaValidator.Object);

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        PaginatedResponse<AliquotaResponse> valor = ok.Value.ShouldBeOfType<PaginatedResponse<AliquotaResponse>>();
        valor.Items.Count.ShouldBe(1);
        valor.TotalItens.ShouldBe(1);
    }

    [Fact]
    public async Task Given_AliquotasMunicipioNaoEncontrado_Should_RetornarNotFound()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        ConfigurarValidacaoComSucesso();
        AliquotaQueryParams queryParams = new();
        _consultaService.Setup(s => s.ListarAliquotasPorMunicipioAsync("0000000", queryParams))
            .ReturnsAsync(Result.Fail<PaginatedResponse<AliquotaResponse>>(
                new NotFoundError("Município com código IBGE '0000000' não encontrado")));

        // Act
        IActionResult resultado = await sut.ListarAliquotasPorMunicipio("0000000", queryParams, _aliquotaValidator.Object);

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("não encontrado");
    }

    [Fact]
    public async Task Given_AliquotasComErroDeValidacaoNoServico_Should_RetornarBadRequest()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        ConfigurarValidacaoComSucesso();
        AliquotaQueryParams queryParams = new();
        _consultaService.Setup(s => s.ListarAliquotasPorMunicipioAsync("3550308", queryParams))
            .ReturnsAsync(Result.Fail<PaginatedResponse<AliquotaResponse>>("Erro genérico no serviço"));

        // Act
        IActionResult resultado = await sut.ListarAliquotasPorMunicipio("3550308", queryParams, _aliquotaValidator.Object);

        // Assert
        BadRequestObjectResult badRequest = resultado.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value!.ToString().ShouldContain("Erro genérico no serviço");
    }

    // ── ObterDetalheAliquota ────────────────────────────────────────

    [Fact]
    public async Task Given_DetalheAliquotaExistente_Should_RetornarOk()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        AliquotaDetalheResponse detalhe = new()
        {
            CodigoMunicipio = "3550308",
            NomeMunicipio = "São Paulo",
            CodigoServico = "010101",
            CodigoServicoFormatado = "01.01.01",
            DescricaoServico = "Serviço teste",
            Aliquota = 5.0m,
            Competencia = "2025-01",
            ColetadoEm = DateTime.UtcNow
        };
        _consultaService.Setup(s => s.ObterDetalheAliquotaAsync("3550308", "01.01.01"))
            .ReturnsAsync(Result.Ok(detalhe));

        // Act
        IActionResult resultado = await sut.ObterDetalheAliquota("3550308", "01.01.01");

        // Assert
        OkObjectResult ok = resultado.ShouldBeOfType<OkObjectResult>();
        AliquotaDetalheResponse valor = ok.Value.ShouldBeOfType<AliquotaDetalheResponse>();
        valor.CodigoMunicipio.ShouldBe("3550308");
        valor.Aliquota.ShouldBe(5.0m);
    }

    [Fact]
    public async Task Given_DetalheAliquotaNaoEncontrada_Should_RetornarNotFound()
    {
        // Arrange
        ConsultaController sut = CriarSut();
        _consultaService.Setup(s => s.ObterDetalheAliquotaAsync("3550308", "99.99.99"))
            .ReturnsAsync(Result.Fail<AliquotaDetalheResponse>(
                new NotFoundError("Alíquota não encontrada")));

        // Act
        IActionResult resultado = await sut.ObterDetalheAliquota("3550308", "99.99.99");

        // Assert
        NotFoundObjectResult notFound = resultado.ShouldBeOfType<NotFoundObjectResult>();
        notFound.Value!.ToString().ShouldContain("Alíquota não encontrada");
    }
}

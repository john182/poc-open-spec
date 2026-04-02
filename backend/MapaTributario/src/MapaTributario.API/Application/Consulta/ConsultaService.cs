using FluentResults;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Application.Crawler;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapaTributario.API.Application.Consulta;

public class ConsultaService : IConsultaService
{
    private readonly IEstadoRepository _estadoRepository;
    private readonly IMunicipioRepository _municipioRepository;
    private readonly IAliquotaRepository _aliquotaRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IExecucaoCrawlerRepository _execucaoCrawlerRepository;
    private readonly ICrawlerExecutionGuard _executionGuard;
    private readonly ICertificadoStore _certificadoStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsultaService> _logger;
    private readonly int _validadeDiasProcessamento;

    public ConsultaService(
        IEstadoRepository estadoRepository,
        IMunicipioRepository municipioRepository,
        IAliquotaRepository aliquotaRepository,
        IServicoRepository servicoRepository,
        IExecucaoCrawlerRepository execucaoCrawlerRepository,
        ICrawlerExecutionGuard executionGuard,
        ICertificadoStore certificadoStore,
        IServiceScopeFactory scopeFactory,
        ILogger<ConsultaService> logger,
        IConfiguration configuration)
    {
        _estadoRepository = estadoRepository;
        _municipioRepository = municipioRepository;
        _aliquotaRepository = aliquotaRepository;
        _servicoRepository = servicoRepository;
        _execucaoCrawlerRepository = execucaoCrawlerRepository;
        _executionGuard = executionGuard;
        _certificadoStore = certificadoStore;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _validadeDiasProcessamento = configuration.GetValue("Crawler:ValidadeDiasProcessamento", 7);
    }

    public async Task<Result<IReadOnlyList<EstadoResponse>>> ListarEstadosAsync()
    {
        var estados = await _estadoRepository.GetAllAsync();

        var response = estados.Select(e => new EstadoResponse
        {
            Sigla = e.Sigla,
            Nome = e.Nome,
            Regiao = e.Regiao
        }).ToList();

        return Result.Ok<IReadOnlyList<EstadoResponse>>(response);
    }

    public async Task<Result<MunicipiosUfResponse>> ListarMunicipiosPorUfAsync(string uf)
    {
        var estado = await _estadoRepository.GetBySiglaAsync(uf);
        if (estado is null)
        {
            return Result.Fail<MunicipiosUfResponse>(
                new NotFoundError($"UF '{uf}' não encontrada"));
        }

        var ultimaExecucao = await _execucaoCrawlerRepository.GetLatestByUfAsync(uf);

        // Caso 1: Nunca processou
        if (ultimaExecucao is null)
        {
            bool disparou = DispararCrawlerSeDisponivel(uf);
            bool semCertificado = !_certificadoStore.HasCertificate();

            return Result.Ok(new MunicipiosUfResponse
            {
                StatusProcessamento = disparou
                    ? StatusProcessamentoUf.ProcessamentoIniciado
                    : StatusProcessamentoUf.AguardandoProcessamento,
                UltimoProcessamento = null,
                Municipios = Array.Empty<MunicipioResponse>(),
                SemCertificado = semCertificado
            });
        }

        // Caso 2: Processando agora
        if (ultimaExecucao.Status == StatusExecucao.EmAndamento)
        {
            var municipiosParciais = await ObterMunicipiosComAliquotaAsync(uf);

            return Result.Ok(new MunicipiosUfResponse
            {
                StatusProcessamento = StatusProcessamentoUf.Processando,
                UltimoProcessamento = null,
                Municipios = municipiosParciais
            });
        }

        // Caso 3 e 4: Concluído ou com falha -- verificar validade
        var municipios = await ObterMunicipiosComAliquotaAsync(uf);

        bool vencido = ultimaExecucao.Fim.HasValue
            && ultimaExecucao.Fim.Value.AddDays(_validadeDiasProcessamento) < DateTime.UtcNow;

        if (vencido)
        {
            // Caso 4: Vencido -- dispara reprocessamento e retorna dados antigos
            bool disparou = DispararCrawlerSeDisponivel(uf);
            bool semCertificado = !_certificadoStore.HasCertificate();

            return Result.Ok(new MunicipiosUfResponse
            {
                StatusProcessamento = disparou
                    ? StatusProcessamentoUf.Atualizando
                    : StatusProcessamentoUf.Vencido,
                UltimoProcessamento = ultimaExecucao.Fim,
                Municipios = municipios,
                SemCertificado = semCertificado
            });
        }

        // Caso 3: Concluído e válido
        return Result.Ok(new MunicipiosUfResponse
        {
            StatusProcessamento = StatusProcessamentoUf.Concluido,
            UltimoProcessamento = ultimaExecucao.Fim,
            Municipios = municipios
        });
    }

    public async Task<Result<PaginatedResponse<AliquotaResponse>>> ListarAliquotasPorMunicipioAsync(
        string codigoIbge,
        AliquotaQueryParams queryParams)
    {
        var municipio = await _municipioRepository.GetByCodigoIbgeAsync(codigoIbge);
        if (municipio is null)
        {
            return Result.Fail<PaginatedResponse<AliquotaResponse>>(
                new NotFoundError($"Município com código IBGE '{codigoIbge}' não encontrado"));
        }

        string? codigoServicoNormalizado = null;
        if (!string.IsNullOrWhiteSpace(queryParams.CodigoServico))
        {
            codigoServicoNormalizado = CodigoServicoNormalizer.NormalizarPrefixo(queryParams.CodigoServico);
            if (string.IsNullOrEmpty(codigoServicoNormalizado))
            {
                return Result.Fail<PaginatedResponse<AliquotaResponse>>(
                    new ValidationError($"Código de serviço '{queryParams.CodigoServico}' em formato inválido. Use ii, ii.ss, ii.ss.dd ou equivalente sem pontos"));
            }
        }

        var (items, total) = await _aliquotaRepository.GetByMunicipioAsync(
            codigoIbge,
            queryParams.Pagina,
            queryParams.TamanhoPagina,
            codigoServicoNormalizado,
            queryParams.Descricao,
            queryParams.AliquotaMin,
            queryParams.AliquotaMax,
            queryParams.Competencia);

        var responseItems = items.Select(a => new AliquotaResponse
        {
            CodigoServico = a.CodigoServico,
            CodigoServicoFormatado = CodigoServicoNormalizer.Formatar(a.CodigoServico),
            DescricaoServico = a.DescricaoServico,
            Aliquota = a.ValorAliquota,
            Competencia = a.Competencia
        }).ToList();

        // Enriquecer descrições vazias com dados da tabela de serviços
        await EnriquecerDescricoesAsync(responseItems);

        return Result.Ok(PaginatedResponse<AliquotaResponse>.Create(
            responseItems,
            queryParams.Pagina,
            queryParams.TamanhoPagina,
            total));
    }

    public async Task<Result<AliquotaDetalheResponse>> ObterDetalheAliquotaAsync(
        string codigoIbge,
        string codigoServico)
    {
        var municipio = await _municipioRepository.GetByCodigoIbgeAsync(codigoIbge);
        if (municipio is null)
        {
            return Result.Fail<AliquotaDetalheResponse>(
                new NotFoundError($"Município com código IBGE '{codigoIbge}' não encontrado"));
        }

        var codigoNormalizado = CodigoServicoNormalizer.Normalizar(codigoServico);
        if (string.IsNullOrEmpty(codigoNormalizado))
        {
            return Result.Fail<AliquotaDetalheResponse>(
                new ValidationError($"Código de serviço '{codigoServico}' em formato inválido. Use ii.ss.dd ou iissdd"));
        }

        var aliquota = await _aliquotaRepository.GetDetalheAsync(codigoIbge, codigoNormalizado);
        if (aliquota is null)
        {
            return Result.Fail<AliquotaDetalheResponse>(
                new NotFoundError($"Alíquota não encontrada para município '{codigoIbge}' e serviço '{codigoServico}'"));
        }

        var detalheResponse = new AliquotaDetalheResponse
        {
            CodigoMunicipio = aliquota.CodigoMunicipio,
            NomeMunicipio = aliquota.NomeMunicipio,
            CodigoServico = aliquota.CodigoServico,
            CodigoServicoFormatado = CodigoServicoNormalizer.Formatar(aliquota.CodigoServico),
            DescricaoServico = aliquota.DescricaoServico,
            Aliquota = aliquota.ValorAliquota,
            Competencia = aliquota.Competencia,
            ColetadoEm = aliquota.ColetadoEm
        };

        // Enriquecer descrição vazia com dados da tabela de serviços
        if (string.IsNullOrWhiteSpace(detalheResponse.DescricaoServico))
        {
            var codigoComPontos = CodigoServicoNormalizer.Formatar(aliquota.CodigoServico);
            var servico = await _servicoRepository.GetByCodigoAsync(codigoComPontos);
            if (servico is not null)
            {
                detalheResponse.DescricaoServico = servico.Descricao;
            }
        }

        return Result.Ok(detalheResponse);
    }

    private async Task<IReadOnlyList<MunicipioResponse>> ObterMunicipiosComAliquotaAsync(string uf)
    {
        var municipios = await _municipioRepository.GetByUfAsync(uf);
        var codigosIbge = municipios.Select(m => m.CodigoIbge);
        var codigosComAliquota = await _aliquotaRepository.ListarCodigosMunicipiosComAliquotaAsync(codigosIbge);

        return municipios
            .Where(m => codigosComAliquota.Contains(m.CodigoIbge))
            .Select(m => new MunicipioResponse
            {
                CodigoIbge = m.CodigoIbge,
                Nome = m.Nome,
                SiglaEstado = m.SiglaEstado,
                PossuiAliquotas = true
            }).ToList();
    }

    private bool DispararCrawlerSeDisponivel(string uf)
    {
        if (!_certificadoStore.HasCertificate())
        {
            _logger.LogWarning(
                "Crawler não disparado para UF {Uf}: nenhum certificado digital disponível", uf);
            return false;
        }

        if (_executionGuard.IsRunning)
        {
            _logger.LogInformation(
                "Crawler já em execução, não é possível iniciar processamento para UF {Uf}", uf);
            return false;
        }

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();

            try
            {
                _logger.LogInformation("Iniciando processamento sob demanda para UF {Uf}", uf);
                var resultado = await crawlerService.ExecutarAsync(
                    TipoExecucao.Manual,
                    forcarReprocessamento: false,
                    filtroUfs: new[] { uf.ToUpperInvariant() });

                if (resultado.IsFailed)
                {
                    _logger.LogWarning("Crawler retornou falha ao processar UF {Uf}: {Errors}",
                        uf, string.Join("; ", resultado.Errors.Select(e => e.Message)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar crawler sob demanda para UF {Uf}", uf);
            }
        });

        return true;
    }

    private async Task EnriquecerDescricoesAsync(List<AliquotaResponse> items)
    {
        var itensVazios = items.Where(i => string.IsNullOrWhiteSpace(i.DescricaoServico)).ToList();
        if (itensVazios.Count == 0)
        {
            return;
        }

        var codigosComPontos = itensVazios
            .Select(i => CodigoServicoNormalizer.Formatar(i.CodigoServico))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();

        var descricoes = await _servicoRepository.ObterDescricoesPorCodigosAsync(codigosComPontos);

        foreach (var item in itensVazios)
        {
            var codigoComPontos = CodigoServicoNormalizer.Formatar(item.CodigoServico);
            if (descricoes.TryGetValue(codigoComPontos, out var descricao))
            {
                item.DescricaoServico = descricao;
            }
        }
    }
}

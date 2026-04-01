using FluentResults;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Application.Errors;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Application.Consulta;

public class ConsultaService : IConsultaService
{
    private readonly IEstadoRepository _estadoRepository;
    private readonly IMunicipioRepository _municipioRepository;
    private readonly IAliquotaRepository _aliquotaRepository;

    public ConsultaService(
        IEstadoRepository estadoRepository,
        IMunicipioRepository municipioRepository,
        IAliquotaRepository aliquotaRepository)
    {
        _estadoRepository = estadoRepository;
        _municipioRepository = municipioRepository;
        _aliquotaRepository = aliquotaRepository;
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

    public async Task<Result<IReadOnlyList<MunicipioResponse>>> ListarMunicipiosPorUfAsync(string uf)
    {
        var estado = await _estadoRepository.GetBySiglaAsync(uf);
        if (estado is null)
        {
            return Result.Fail<IReadOnlyList<MunicipioResponse>>(
                new NotFoundError($"UF '{uf}' não encontrada"));
        }

        var municipios = await _municipioRepository.GetByUfAsync(uf);

        var response = municipios.Select(m => new MunicipioResponse
        {
            CodigoIbge = m.CodigoIbge,
            Nome = m.Nome,
            SiglaEstado = m.SiglaEstado
        }).ToList();

        return Result.Ok<IReadOnlyList<MunicipioResponse>>(response);
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
            ValorAliquota = a.ValorAliquota,
            Competencia = a.Competencia
        }).ToList();

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

        return Result.Ok(new AliquotaDetalheResponse
        {
            CodigoMunicipio = aliquota.CodigoMunicipio,
            NomeMunicipio = aliquota.NomeMunicipio,
            CodigoServico = aliquota.CodigoServico,
            CodigoServicoFormatado = CodigoServicoNormalizer.Formatar(aliquota.CodigoServico),
            DescricaoServico = aliquota.DescricaoServico,
            ValorAliquota = aliquota.ValorAliquota,
            Competencia = aliquota.Competencia,
            ColetadoEm = aliquota.ColetadoEm
        });
    }
}

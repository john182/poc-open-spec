using FluentValidation;
using MapaTributario.API.Application.Consulta;
using MapaTributario.API.Application.Consulta.Contracts;
using MapaTributario.API.Application.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapaTributario.API.Controllers;

/// <summary>
/// Endpoints de consulta de estados, municipios e aliquotas.
/// </summary>
[ApiController]
[Route("api/v1")]
[Authorize]
public class ConsultaController : ControllerBase
{
    private readonly IConsultaService _consultaService;

    public ConsultaController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    /// <summary>
    /// Lista todos os 27 estados brasileiros ordenados por nome.
    /// </summary>
    /// <returns>Lista de estados com sigla, nome e regiao.</returns>
    /// <response code="200">Lista de estados retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    [HttpGet("estados")]
    [ProducesResponseType(typeof(IReadOnlyList<EstadoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListarEstados()
    {
        var result = await _consultaService.ListarEstadosAsync();
        return Ok(result.Value);
    }

    /// <summary>
    /// Lista municipios de um estado pela sigla da UF.
    /// </summary>
    /// <param name="uf">Sigla da UF (ex: SP, RJ, MG).</param>
    /// <returns>Lista de municipios do estado.</returns>
    /// <response code="200">Lista de municipios retornada com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">UF nao encontrada.</response>
    [HttpGet("estados/{uf}/municipios")]
    [ProducesResponseType(typeof(IReadOnlyList<MunicipioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarMunicipiosPorUf([FromRoute] string uf)
    {
        var result = await _consultaService.ListarMunicipiosPorUfAsync(uf);
        if (result.IsFailed)
        {
            return NotFound(new { erro = result.Errors.First().Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lista aliquotas de um municipio com paginacao e filtros.
    /// </summary>
    /// <param name="codigoIbge">Codigo IBGE do municipio (7 digitos).</param>
    /// <param name="queryParams">Parametros de paginacao e filtros.</param>
    /// <param name="validator">Validador injetado pelo container.</param>
    /// <returns>Resposta paginada com aliquotas.</returns>
    /// <response code="200">Lista paginada de aliquotas retornada com sucesso.</response>
    /// <response code="400">Parametros de consulta invalidos.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Municipio nao encontrado.</response>
    [HttpGet("municipios/{codigoIbge}/aliquotas")]
    [ProducesResponseType(typeof(PaginatedResponse<AliquotaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarAliquotasPorMunicipio(
        [FromRoute] string codigoIbge,
        [FromQuery] AliquotaQueryParams queryParams,
        [FromServices] IValidator<AliquotaQueryParams> validator)
    {
        var validation = await validator.ValidateAsync(queryParams);
        if (!validation.IsValid)
        {
            return BadRequest(new { erro = "Validação falhou", detalhes = validation.Errors.Select(e => e.ErrorMessage).ToArray() });
        }

        var result = await _consultaService.ListarAliquotasPorMunicipioAsync(codigoIbge, queryParams);
        if (result.IsFailed)
        {
            if (result.Errors.OfType<NotFoundError>().Any())
            {
                return NotFound(new { erro = result.Errors.First().Message });
            }

            return BadRequest(new { erro = result.Errors.First().Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Retorna detalhe de uma aliquota especifica para um municipio e servico.
    /// </summary>
    /// <param name="codigoIbge">Codigo IBGE do municipio (7 digitos).</param>
    /// <param name="codigoServico">Codigo do servico (formato ii.ss.dd ou iissdd).</param>
    /// <returns>Detalhe da aliquota.</returns>
    /// <response code="200">Detalhe da aliquota retornado com sucesso.</response>
    /// <response code="401">Token JWT ausente ou invalido.</response>
    /// <response code="404">Municipio ou aliquota nao encontrada.</response>
    [HttpGet("municipios/{codigoIbge}/aliquotas/{codigoServico}")]
    [ProducesResponseType(typeof(AliquotaDetalheResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterDetalheAliquota(
        [FromRoute] string codigoIbge,
        [FromRoute] string codigoServico)
    {
        var result = await _consultaService.ObterDetalheAliquotaAsync(codigoIbge, codigoServico);
        if (result.IsFailed)
        {
            return NotFound(new { erro = result.Errors.First().Message });
        }

        return Ok(result.Value);
    }
}

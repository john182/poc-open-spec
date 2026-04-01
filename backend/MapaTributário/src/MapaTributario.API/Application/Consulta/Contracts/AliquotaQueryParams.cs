namespace MapaTributario.API.Application.Consulta.Contracts;

public class AliquotaQueryParams
{
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
    public string? CodigoServico { get; set; }
    public string? Descricao { get; set; }
    public decimal? AliquotaMin { get; set; }
    public decimal? AliquotaMax { get; set; }
    public string? Competencia { get; set; }
}

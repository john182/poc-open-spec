namespace MapaTributario.API.Application.Consulta.Contracts;

public class AliquotaResponse
{
    public string CodigoServico { get; set; } = null!;
    public string CodigoServicoFormatado { get; set; } = null!;
    public string DescricaoServico { get; set; } = null!;
    public decimal ValorAliquota { get; set; }
    public string Competencia { get; set; } = null!;
}

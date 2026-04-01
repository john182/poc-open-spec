namespace MapaTributario.API.Application.Consulta.Contracts;

public class AliquotaDetalheResponse
{
    public string CodigoMunicipio { get; set; } = null!;
    public string NomeMunicipio { get; set; } = null!;
    public string CodigoServico { get; set; } = null!;
    public string CodigoServicoFormatado { get; set; } = null!;
    public string DescricaoServico { get; set; } = null!;
    public decimal Aliquota { get; set; }
    public string Competencia { get; set; } = null!;
    public DateTime ColetadoEm { get; set; }
}

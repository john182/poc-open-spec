namespace MapaTributario.API.Infrastructure.External.Contracts;

public class AliquotaNfseResponse
{
    public decimal Aliquota { get; set; }
    public string CodigoServico { get; set; } = null!;
    public string DescricaoServico { get; set; } = null!;
}

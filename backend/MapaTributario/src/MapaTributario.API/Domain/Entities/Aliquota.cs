namespace MapaTributario.API.Domain.Entities;

public class Aliquota
{
    public string Id { get; private set; } = null!;
    public string CodigoMunicipio { get; private set; } = null!;
    public string NomeMunicipio { get; private set; } = null!;
    public string CodigoServico { get; private set; } = null!;
    public string CodigoServicoFormatado { get; private set; } = null!;
    public string DescricaoServico { get; private set; } = null!;
    public decimal ValorAliquota { get; private set; }
    public string Competencia { get; private set; } = null!;
    public DateTime ColetadoEm { get; private set; }
    public string Fonte { get; private set; } = null!;

    private Aliquota() { }

    public static Aliquota Create(
        string codigoMunicipio,
        string nomeMunicipio,
        string codigoServico,
        string codigoServicoFormatado,
        string descricaoServico,
        decimal valorAliquota,
        string competencia,
        string fonte)
    {
        return new Aliquota
        {
            CodigoMunicipio = codigoMunicipio,
            NomeMunicipio = nomeMunicipio,
            CodigoServico = codigoServico,
            CodigoServicoFormatado = codigoServicoFormatado,
            DescricaoServico = descricaoServico,
            ValorAliquota = valorAliquota,
            Competencia = competencia,
            ColetadoEm = DateTime.UtcNow,
            Fonte = fonte
        };
    }

    public void SetId(string id) => Id = id;

    public void UpdateAliquota(decimal valorAliquota, string competencia)
    {
        ValorAliquota = valorAliquota;
        Competencia = competencia;
        ColetadoEm = DateTime.UtcNow;
    }
}

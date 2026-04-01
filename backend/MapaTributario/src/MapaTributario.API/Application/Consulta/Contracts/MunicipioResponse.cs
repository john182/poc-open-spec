namespace MapaTributario.API.Application.Consulta.Contracts;

public class MunicipioResponse
{
    public string CodigoIbge { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string SiglaEstado { get; set; } = null!;
    public bool PossuiAliquotas { get; set; }
}

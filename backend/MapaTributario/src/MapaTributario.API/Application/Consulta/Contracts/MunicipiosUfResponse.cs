namespace MapaTributario.API.Application.Consulta.Contracts;

public class MunicipiosUfResponse
{
    public string StatusProcessamento { get; set; } = null!;
    public DateTime? UltimoProcessamento { get; set; }
    public IReadOnlyList<MunicipioResponse> Municipios { get; set; } = Array.Empty<MunicipioResponse>();
}

public static class StatusProcessamentoUf
{
    public const string NaoProcessado = "naoProcessado";
    public const string Processando = "processando";
    public const string Concluido = "concluido";
    public const string Vencido = "vencido";
    public const string ProcessamentoIniciado = "processamentoIniciado";
    public const string Atualizando = "atualizando";
    public const string AguardandoProcessamento = "aguardandoProcessamento";
}

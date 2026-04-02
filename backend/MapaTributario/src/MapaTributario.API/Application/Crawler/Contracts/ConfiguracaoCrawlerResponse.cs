namespace MapaTributario.API.Application.Crawler.Contracts;

public class ConfiguracaoCrawlerResponse
{
    public string Id { get; set; } = null!;
    public string CronSchedule { get; set; } = null!;
    public int LimiteRequisicoesPorSegundo { get; set; }
    public int OrcamentoDiario { get; set; }
    public int TamanheLoteCertificado { get; set; }
    public int PausaLoteSegundos { get; set; }
    public int TamanheLoteMongo { get; set; }
    public int MaxTentativas { get; set; }
    public int LimiteParadaAntecipada { get; set; }
    public int MaxDesdobramento { get; set; }
    public int MaxDetalhamento { get; set; }
    public int MaxFalhasConsecutivasDetalhamento { get; set; }
    public int MaxFalhasConsecutivasDesdobramento { get; set; }
    public int MaxItensParalelos { get; set; }
    public List<string> CodigosSondagem { get; set; } = new();
    public int ValidadeDiasProcessamento { get; set; }
    public int CircuitBreakerLimiarErroPercent { get; set; }
    public int CircuitBreakerJanelaAvaliacaoSegundos { get; set; }
    public int CircuitBreakerPausaSegundos { get; set; }
    public int CircuitBreakerAmostraMinima { get; set; }
    public bool Ativo { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

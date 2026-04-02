namespace MapaTributario.API.Domain.Entities;

public class ConfiguracaoCrawler
{
    public string Id { get; private set; } = null!;

    // Agendamento
    public string CronSchedule { get; private set; } = "0 2 * * *";

    // Limites de requisição
    public int LimiteRequisicoesPorSegundo { get; private set; } = 15;
    public int OrcamentoDiario { get; private set; } = 50000;

    // Lote de certificado (CertificateProtection)
    public int TamanheLoteCertificado { get; private set; } = 200;
    public int PausaLoteSegundos { get; private set; } = 5;

    // Lote de consulta MongoDB
    public int TamanheLoteMongo { get; private set; } = 50;

    // Crawler Service - parâmetros de processamento
    public int MaxTentativas { get; private set; } = 3;
    public int LimiteParadaAntecipada { get; private set; } = 9;
    public int MaxDesdobramento { get; private set; } = 20;
    public int MaxDetalhamento { get; private set; } = 99;
    public int MaxFalhasConsecutivasDetalhamento { get; private set; } = 2;
    public int MaxFalhasConsecutivasDesdobramento { get; private set; } = 2;
    public int MaxItensParalelos { get; private set; } = 10;

    // Códigos de sondagem (probe)
    public List<string> CodigosSondagem { get; private set; } = new()
    {
        "01.01.01", "07.02.01", "14.01.01", "17.01.01", "25.01.01"
    };

    // Validade de processamento
    public int ValidadeDiasProcessamento { get; private set; } = 7;

    // Circuit Breaker
    public int CircuitBreakerLimiarErroPercent { get; private set; } = 50;
    public int CircuitBreakerJanelaAvaliacaoSegundos { get; private set; } = 60;
    public int CircuitBreakerPausaSegundos { get; private set; } = 300;
    public int CircuitBreakerAmostraMinima { get; private set; } = 10;

    // Controle
    public bool Ativo { get; private set; } = true;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private ConfiguracaoCrawler() { }

    public static ConfiguracaoCrawler CriarPadrao()
    {
        DateTime agora = DateTime.UtcNow;
        return new ConfiguracaoCrawler
        {
            CronSchedule = "0 2 * * *",
            LimiteRequisicoesPorSegundo = 15,
            OrcamentoDiario = 50000,
            TamanheLoteCertificado = 200,
            PausaLoteSegundos = 5,
            TamanheLoteMongo = 50,
            MaxTentativas = 3,
            LimiteParadaAntecipada = 9,
            MaxDesdobramento = 20,
            MaxDetalhamento = 99,
            MaxFalhasConsecutivasDetalhamento = 2,
            MaxFalhasConsecutivasDesdobramento = 2,
            MaxItensParalelos = 10,
            CodigosSondagem = new List<string>
            {
                "01.01.01", "07.02.01", "14.01.01", "17.01.01", "25.01.01"
            },
            ValidadeDiasProcessamento = 7,
            CircuitBreakerLimiarErroPercent = 50,
            CircuitBreakerJanelaAvaliacaoSegundos = 60,
            CircuitBreakerPausaSegundos = 300,
            CircuitBreakerAmostraMinima = 10,
            Ativo = true,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }

    public void SetId(string id) => Id = id;

    public void MarcarAtualizado()
    {
        AtualizadoEm = DateTime.UtcNow;
    }
}

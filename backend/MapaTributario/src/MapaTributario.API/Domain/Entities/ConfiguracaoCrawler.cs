namespace MapaTributario.API.Domain.Entities;

public class ConfiguracaoCrawler
{
    public string Id { get; private set; } = null!;

    // Agendamento
    public string CronSchedule { get; private set; } = "0 2 * * *";

    // Limites de requisição (valores otimizados conforme benchmark — API sem rate limiting, plateau ~17 req/s serial)
    public int LimiteRequisicoesPorSegundo { get; private set; } = 50;
    public int OrcamentoDiario { get; private set; } = 200000;

    // Lote de certificado (CertificateProtection)
    public int TamanhoLoteCertificado { get; private set; } = 500;
    public int PausaLoteSegundos { get; private set; } = 0;

    // Lote de consulta MongoDB
    public int TamanhoLoteMongo { get; private set; } = 50;

    // Crawler Service - parâmetros de processamento
    public int MaxTentativas { get; private set; } = 3;
    public int LimiteParadaAntecipada { get; private set; } = 9;
    public int MaxDesdobramento { get; private set; } = 20;
    public int MaxDetalhamento { get; private set; } = 99;
    public int MaxFalhasConsecutivasDetalhamento { get; private set; } = 2;
    public int MaxFalhasConsecutivasDesdobramento { get; private set; } = 2;
    public int MaxItensParalelos { get; private set; } = 20;

    // Paralelismo por UF (quantas UFs simultâneas na Fase 1)
    public int MaxUfsParalelas { get; private set; } = 5;

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
            LimiteRequisicoesPorSegundo = 50,
            OrcamentoDiario = 200000,
            TamanhoLoteCertificado = 500,
            PausaLoteSegundos = 0,
            TamanhoLoteMongo = 50,
            MaxTentativas = 3,
            LimiteParadaAntecipada = 9,
            MaxDesdobramento = 20,
            MaxDetalhamento = 99,
            MaxFalhasConsecutivasDetalhamento = 2,
            MaxFalhasConsecutivasDesdobramento = 2,
            MaxItensParalelos = 20,
            MaxUfsParalelas = 5,
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

    public void Atualizar(
        string cronSchedule,
        int limiteRequisicoesPorSegundo,
        int orcamentoDiario,
        int tamanhoLoteCertificado,
        int pausaLoteSegundos,
        int tamanhoLoteMongo,
        int maxTentativas,
        int limiteParadaAntecipada,
        int maxDesdobramento,
        int maxDetalhamento,
        int maxFalhasConsecutivasDetalhamento,
        int maxFalhasConsecutivasDesdobramento,
        int maxItensParalelos,
        int maxUfsParalelas,
        List<string> codigosSondagem,
        int validadeDiasProcessamento,
        int circuitBreakerLimiarErroPercent,
        int circuitBreakerJanelaAvaliacaoSegundos,
        int circuitBreakerPausaSegundos,
        int circuitBreakerAmostraMinima,
        bool ativo)
    {
        CronSchedule = cronSchedule;
        LimiteRequisicoesPorSegundo = limiteRequisicoesPorSegundo;
        OrcamentoDiario = orcamentoDiario;
        TamanhoLoteCertificado = tamanhoLoteCertificado;
        PausaLoteSegundos = pausaLoteSegundos;
        TamanhoLoteMongo = tamanhoLoteMongo;
        MaxTentativas = maxTentativas;
        LimiteParadaAntecipada = limiteParadaAntecipada;
        MaxDesdobramento = maxDesdobramento;
        MaxDetalhamento = maxDetalhamento;
        MaxFalhasConsecutivasDetalhamento = maxFalhasConsecutivasDetalhamento;
        MaxFalhasConsecutivasDesdobramento = maxFalhasConsecutivasDesdobramento;
        MaxItensParalelos = maxItensParalelos;
        MaxUfsParalelas = maxUfsParalelas;
        CodigosSondagem = codigosSondagem;
        ValidadeDiasProcessamento = validadeDiasProcessamento;
        CircuitBreakerLimiarErroPercent = circuitBreakerLimiarErroPercent;
        CircuitBreakerJanelaAvaliacaoSegundos = circuitBreakerJanelaAvaliacaoSegundos;
        CircuitBreakerPausaSegundos = circuitBreakerPausaSegundos;
        CircuitBreakerAmostraMinima = circuitBreakerAmostraMinima;
        Ativo = ativo;
        MarcarAtualizado();
    }

    public void AtualizarParcial(
        string? cronSchedule = null,
        int? limiteRequisicoesPorSegundo = null,
        int? orcamentoDiario = null,
        int? tamanhoLoteCertificado = null,
        int? pausaLoteSegundos = null,
        int? tamanhoLoteMongo = null,
        int? maxTentativas = null,
        int? limiteParadaAntecipada = null,
        int? maxDesdobramento = null,
        int? maxDetalhamento = null,
        int? maxFalhasConsecutivasDetalhamento = null,
        int? maxFalhasConsecutivasDesdobramento = null,
        int? maxItensParalelos = null,
        int? maxUfsParalelas = null,
        List<string>? codigosSondagem = null,
        int? validadeDiasProcessamento = null,
        int? circuitBreakerLimiarErroPercent = null,
        int? circuitBreakerJanelaAvaliacaoSegundos = null,
        int? circuitBreakerPausaSegundos = null,
        int? circuitBreakerAmostraMinima = null,
        bool? ativo = null)
    {
        if (cronSchedule is not null) CronSchedule = cronSchedule;
        if (limiteRequisicoesPorSegundo.HasValue) LimiteRequisicoesPorSegundo = limiteRequisicoesPorSegundo.Value;
        if (orcamentoDiario.HasValue) OrcamentoDiario = orcamentoDiario.Value;
        if (tamanhoLoteCertificado.HasValue) TamanhoLoteCertificado = tamanhoLoteCertificado.Value;
        if (pausaLoteSegundos.HasValue) PausaLoteSegundos = pausaLoteSegundos.Value;
        if (tamanhoLoteMongo.HasValue) TamanhoLoteMongo = tamanhoLoteMongo.Value;
        if (maxTentativas.HasValue) MaxTentativas = maxTentativas.Value;
        if (limiteParadaAntecipada.HasValue) LimiteParadaAntecipada = limiteParadaAntecipada.Value;
        if (maxDesdobramento.HasValue) MaxDesdobramento = maxDesdobramento.Value;
        if (maxDetalhamento.HasValue) MaxDetalhamento = maxDetalhamento.Value;
        if (maxFalhasConsecutivasDetalhamento.HasValue) MaxFalhasConsecutivasDetalhamento = maxFalhasConsecutivasDetalhamento.Value;
        if (maxFalhasConsecutivasDesdobramento.HasValue) MaxFalhasConsecutivasDesdobramento = maxFalhasConsecutivasDesdobramento.Value;
        if (maxItensParalelos.HasValue) MaxItensParalelos = maxItensParalelos.Value;
        if (maxUfsParalelas.HasValue) MaxUfsParalelas = maxUfsParalelas.Value;
        if (codigosSondagem is not null) CodigosSondagem = codigosSondagem;
        if (validadeDiasProcessamento.HasValue) ValidadeDiasProcessamento = validadeDiasProcessamento.Value;
        if (circuitBreakerLimiarErroPercent.HasValue) CircuitBreakerLimiarErroPercent = circuitBreakerLimiarErroPercent.Value;
        if (circuitBreakerJanelaAvaliacaoSegundos.HasValue) CircuitBreakerJanelaAvaliacaoSegundos = circuitBreakerJanelaAvaliacaoSegundos.Value;
        if (circuitBreakerPausaSegundos.HasValue) CircuitBreakerPausaSegundos = circuitBreakerPausaSegundos.Value;
        if (circuitBreakerAmostraMinima.HasValue) CircuitBreakerAmostraMinima = circuitBreakerAmostraMinima.Value;
        if (ativo.HasValue) Ativo = ativo.Value;
        MarcarAtualizado();
    }
}

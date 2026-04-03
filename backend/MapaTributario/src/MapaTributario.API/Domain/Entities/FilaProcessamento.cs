namespace MapaTributario.API.Domain.Entities;

public class FilaProcessamento
{
    public string Id { get; private set; } = null!;
    public string CodigoMunicipio { get; private set; } = null!;
    public string CodigoServico { get; private set; } = null!;
    public string Competencia { get; private set; } = null!;
    public string Uf { get; private set; } = null!;
    public StatusFila Status { get; private set; }
    public int Tentativas { get; private set; }
    public string? UltimoErro { get; private set; }
    public DateTime? ProximaTentativa { get; private set; }
    public string ExecucaoId { get; private set; } = null!;
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private FilaProcessamento() { }

    public static FilaProcessamento Create(
        string codigoMunicipio,
        string codigoServico,
        string competencia,
        string execucaoId,
        string uf)
    {
        DateTime agora = DateTime.UtcNow;
        return new FilaProcessamento
        {
            CodigoMunicipio = codigoMunicipio,
            CodigoServico = codigoServico,
            Competencia = competencia,
            Uf = uf.ToUpperInvariant(),
            Status = StatusFila.Pendente,
            Tentativas = 0,
            ExecucaoId = execucaoId,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }

    public void SetId(string id) => Id = id;

    public void MarcarProcessando()
    {
        Status = StatusFila.Processando;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void MarcarConcluido()
    {
        Status = StatusFila.Concluido;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void MarcarErro(string erro, int maxTentativas, int baseDelaySeconds = 30, int backoffMultiplier = 4)
    {
        Tentativas++;
        UltimoErro = erro;
        AtualizadoEm = DateTime.UtcNow;

        if (Tentativas >= maxTentativas)
        {
            Status = StatusFila.Erro;
            ProximaTentativa = null;
        }
        else
        {
            Status = StatusFila.Erro;
            int delaySeconds = baseDelaySeconds * (int)Math.Pow(backoffMultiplier, Tentativas - 1);
            ProximaTentativa = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }

    public void ReverterParaPendente()
    {
        Status = StatusFila.Pendente;
        AtualizadoEm = DateTime.UtcNow;
    }

    public bool PodeRetentar(int maxTentativas) => Tentativas < maxTentativas;
}

public enum StatusFila
{
    Pendente,
    Processando,
    Concluido,
    Erro
}

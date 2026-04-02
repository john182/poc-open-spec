namespace MapaTributario.API.Domain.Entities;

public class ExecucaoCrawler
{
    public string Id { get; private set; } = null!;
    public DateTime Inicio { get; private set; }
    public DateTime? Fim { get; private set; }
    public StatusExecucao Status { get; private set; }
    public TipoExecucao Tipo { get; private set; }
    public int TotalMunicipios { get; private set; }
    public int TotalServicos { get; private set; }

    private int _processados;
    public int Processados { get => _processados; private set => _processados = value; }

    private int _erros;
    public int Erros { get => _erros; private set => _erros = value; }

    public List<string> DetalhesErro { get; private set; } = new();
    public List<string> UfsProcessadas { get; private set; } = new();
    public string? UfAtual { get; private set; }
    public Dictionary<string, ProgressoUf> ProgressoUfs { get; private set; } = new();

    private ExecucaoCrawler() { }

    public static ExecucaoCrawler Create(TipoExecucao tipo)
    {
        return new ExecucaoCrawler
        {
            Inicio = DateTime.UtcNow,
            Status = StatusExecucao.EmAndamento,
            Tipo = tipo,
            TotalMunicipios = 0,
            TotalServicos = 0,
            Processados = 0,
            Erros = 0,
            DetalhesErro = new List<string>(),
            UfsProcessadas = new List<string>(),
            UfAtual = null,
            ProgressoUfs = new Dictionary<string, ProgressoUf>()
        };
    }

    public void SetUfsProcessadas(IEnumerable<string> ufs)
    {
        UfsProcessadas = ufs.Select(u => u.ToUpperInvariant()).Distinct().ToList();
    }

    public void SetId(string id) => Id = id;

    public void SetTotais(int totalMunicipios, int totalServicos)
    {
        TotalMunicipios = totalMunicipios;
        TotalServicos = totalServicos;
    }

    private readonly object _lock = new();

    public void IncrementarProcessados() => Interlocked.Increment(ref _processados);

    public void IncrementarErros(string detalhe)
    {
        Interlocked.Increment(ref _erros);
        lock (_lock)
        {
            DetalhesErro.Add(detalhe);
        }
    }

    public void Finalizar(StatusExecucao status)
    {
        Status = status;
        Fim = DateTime.UtcNow;
    }

    public void IniciarProcessamentoUf(string uf)
    {
        UfAtual = uf.ToUpperInvariant();
        if (!ProgressoUfs.ContainsKey(UfAtual))
        {
            ProgressoUfs[UfAtual] = new ProgressoUf
            {
                Uf = UfAtual,
                Status = "EmAndamento",
                Inicio = DateTime.UtcNow
            };
        }
        else
        {
            ProgressoUfs[UfAtual].Status = "EmAndamento";
            ProgressoUfs[UfAtual].Inicio = DateTime.UtcNow;
        }
    }

    public void FinalizarProcessamentoUf(string uf, int municipiosEncontrados)
    {
        string ufNormalizada = uf.ToUpperInvariant();
        if (ProgressoUfs.ContainsKey(ufNormalizada))
        {
            ProgressoUfs[ufNormalizada].Status = "Concluido";
            ProgressoUfs[ufNormalizada].MunicipiosEncontrados = municipiosEncontrados;
            ProgressoUfs[ufNormalizada].Fim = DateTime.UtcNow;
        }

        // Limpar UfAtual se era esta UF
        if (UfAtual == ufNormalizada)
        {
            UfAtual = null;
        }
    }
}

public enum StatusExecucao
{
    EmAndamento,
    Concluido,
    FalhaParcial,
    Falha
}

public enum TipoExecucao
{
    Agendado,
    Manual
}

public class ProgressoUf
{
    public string Uf { get; set; } = null!;
    public string Status { get; set; } = "Pendente";
    public int MunicipiosEncontrados { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}

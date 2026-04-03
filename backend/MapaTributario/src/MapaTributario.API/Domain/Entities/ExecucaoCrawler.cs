using System.Collections.Concurrent;

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
    public List<string> UfsEmAndamento { get; private set; } = new();
    public ConcurrentDictionary<string, ProgressoUf> ProgressoUfs { get; private set; } = new();

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
            UfsEmAndamento = new List<string>(),
            ProgressoUfs = new ConcurrentDictionary<string, ProgressoUf>()
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
        string ufNormalizada = uf.ToUpperInvariant();

        ProgressoUfs.AddOrUpdate(
            ufNormalizada,
            _ => new ProgressoUf
            {
                Uf = ufNormalizada,
                Status = StatusProgressoUf.EmAndamento,
                Inicio = DateTime.UtcNow
            },
            (_, existente) =>
            {
                existente.Status = StatusProgressoUf.EmAndamento;
                existente.Inicio = DateTime.UtcNow;
                return existente;
            });

        lock (_lock)
        {
            if (!UfsEmAndamento.Contains(ufNormalizada))
            {
                UfsEmAndamento.Add(ufNormalizada);
            }
        }
    }

    public void FinalizarProcessamentoUf(string uf, int municipiosEncontrados, int municipiosAtivos)
    {
        string ufNormalizada = uf.ToUpperInvariant();
        if (ProgressoUfs.TryGetValue(ufNormalizada, out ProgressoUf? progresso))
        {
            progresso.Status = StatusProgressoUf.Concluido;
            progresso.MunicipiosEncontrados = municipiosEncontrados;
            progresso.MunicipiosAtivos = municipiosAtivos;
            progresso.Fim = DateTime.UtcNow;
        }

        lock (_lock)
        {
            UfsEmAndamento.Remove(ufNormalizada);
        }
    }

    public void FalharProcessamentoUf(string uf, int municipiosEncontrados)
    {
        string ufNormalizada = uf.ToUpperInvariant();
        if (ProgressoUfs.TryGetValue(ufNormalizada, out ProgressoUf? progresso))
        {
            progresso.Status = StatusProgressoUf.Falha;
            progresso.MunicipiosEncontrados = municipiosEncontrados;
            progresso.MunicipiosAtivos = 0;
            progresso.Fim = DateTime.UtcNow;
        }

        lock (_lock)
        {
            UfsEmAndamento.Remove(ufNormalizada);
        }
    }

    public void InterromperProcessamentoUf(string uf, int municipiosEncontrados, int municipiosAtivosAteAgora)
    {
        string ufNormalizada = uf.ToUpperInvariant();
        if (ProgressoUfs.TryGetValue(ufNormalizada, out ProgressoUf? progresso))
        {
            progresso.Status = StatusProgressoUf.Interrompido;
            progresso.MunicipiosEncontrados = municipiosEncontrados;
            progresso.MunicipiosAtivos = municipiosAtivosAteAgora;
            progresso.Fim = DateTime.UtcNow;
        }

        lock (_lock)
        {
            UfsEmAndamento.Remove(ufNormalizada);
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
    public StatusProgressoUf Status { get; set; } = StatusProgressoUf.Pendente;
    public int MunicipiosEncontrados { get; set; }
    public int MunicipiosAtivos { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}

public enum StatusProgressoUf
{
    Pendente,
    EmAndamento,
    Concluido,
    Falha,
    Interrompido
}

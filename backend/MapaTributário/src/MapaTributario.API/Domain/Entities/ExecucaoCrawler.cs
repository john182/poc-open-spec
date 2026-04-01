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
    public int Processados { get; private set; }
    public int Erros { get; private set; }
    public List<string> DetalhesErro { get; private set; } = new();

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
            DetalhesErro = new List<string>()
        };
    }

    public void SetId(string id) => Id = id;

    public void SetTotais(int totalMunicipios, int totalServicos)
    {
        TotalMunicipios = totalMunicipios;
        TotalServicos = totalServicos;
    }

    public void IncrementarProcessados() => Processados++;

    public void IncrementarErros(string detalhe)
    {
        Erros++;
        DetalhesErro.Add(detalhe);
    }

    public void Finalizar(StatusExecucao status)
    {
        Status = status;
        Fim = DateTime.UtcNow;
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

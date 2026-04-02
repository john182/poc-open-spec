namespace MapaTributario.API.Domain.Entities;

public class Municipio
{
    public string Id { get; private set; } = null!;
    public string CodigoIbge { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string SiglaEstado { get; private set; } = null!;
    public bool EhCapital { get; private set; }

    private Municipio() { }

    public static Municipio Create(string codigoIbge, string nome, string siglaEstado, bool ehCapital = false)
    {
        return new Municipio
        {
            CodigoIbge = codigoIbge,
            Nome = nome,
            SiglaEstado = siglaEstado,
            EhCapital = ehCapital
        };
    }

    public void SetId(string id) => Id = id;
}

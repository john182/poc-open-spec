namespace MapaTributario.API.Domain.Entities;

public class Estado
{
    public string Id { get; private set; } = null!;
    public string Sigla { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Regiao { get; private set; } = null!;

    private Estado() { }

    public static Estado Create(string sigla, string nome, string regiao)
    {
        return new Estado
        {
            Sigla = sigla,
            Nome = nome,
            Regiao = regiao
        };
    }

    public void SetId(string id) => Id = id;
}

namespace MapaTributario.API.Domain.Entities;

public class Servico
{
    public string Id { get; private set; } = null!;
    public string CodigoTribNac { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string Item { get; private set; } = null!;
    public string Subitem { get; private set; } = null!;
    public string Desdobramento { get; private set; } = null!;

    private Servico() { }

    public static Servico Create(
        string codigoTribNac,
        string descricao,
        string item,
        string subitem,
        string desdobramento)
    {
        return new Servico
        {
            CodigoTribNac = codigoTribNac,
            Descricao = descricao,
            Item = item,
            Subitem = subitem,
            Desdobramento = desdobramento
        };
    }

    public void SetId(string id) => Id = id;
}

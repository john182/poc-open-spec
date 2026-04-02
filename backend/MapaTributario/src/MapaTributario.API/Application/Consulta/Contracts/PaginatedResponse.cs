namespace MapaTributario.API.Application.Consulta.Contracts;

public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Pagina { get; set; }
    public int TamanhoPagina { get; set; }
    public long TotalItens { get; set; }
    public int TotalPaginas { get; set; }

    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> items,
        int pagina,
        int tamanhoPagina,
        long totalItens)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina,
            TotalItens = totalItens,
            TotalPaginas = tamanhoPagina > 0 ? (int)Math.Ceiling((double)totalItens / tamanhoPagina) : 0
        };
    }
}

namespace MapaTributario.API.Application.Perfil.Contracts;

public class AtualizarPerfilRequest
{
    public string Nome { get; set; } = null!;
    public string? SenhaAtual { get; set; }
    public string? NovaSenha { get; set; }
}

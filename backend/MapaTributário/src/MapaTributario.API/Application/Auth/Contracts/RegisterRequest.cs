namespace MapaTributario.API.Application.Auth.Contracts;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public string Senha { get; set; } = null!;
}

namespace MapaTributario.API.Application.Auth.Contracts;

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Senha { get; set; } = null!;
}

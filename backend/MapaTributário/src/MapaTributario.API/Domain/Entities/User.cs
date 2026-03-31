namespace MapaTributario.API.Domain.Entities;

public class User
{
    public string Id { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public DateTime DataCriacao { get; private set; }
    public bool Ativo { get; private set; }

    private User() { }

    public static User Create(string email, string nome, string passwordHash)
    {
        return new User
        {
            Email = email,
            Nome = nome,
            PasswordHash = passwordHash,
            DataCriacao = DateTime.UtcNow,
            Ativo = true
        };
    }

    public void SetId(string id) => Id = id;

    public void Deactivate() => Ativo = false;
}

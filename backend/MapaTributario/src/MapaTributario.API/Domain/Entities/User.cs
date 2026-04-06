namespace MapaTributario.API.Domain.Entities;

public class User
{
    public string Id { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public string Role { get; private set; } = "User";
    public DateTime DataCriacao { get; private set; }
    public bool Ativo { get; private set; }

    private User() { }

    public static User Create(string email, string nome, string passwordHash, string role = "User")
    {
        return new User
        {
            Email = email,
            Nome = nome,
            PasswordHash = passwordHash,
            Role = role,
            DataCriacao = DateTime.UtcNow,
            Ativo = true
        };
    }

    public void SetId(string id) => Id = id;

    public void Deactivate() => Ativo = false;

    public void AtualizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome não pode ser vazio.", nameof(nome));

        Nome = nome;
    }

    public void AtualizarSenha(string novoHash)
    {
        if (string.IsNullOrWhiteSpace(novoHash))
            throw new ArgumentException("Hash da senha não pode ser vazio.", nameof(novoHash));

        PasswordHash = novoHash;
    }
}

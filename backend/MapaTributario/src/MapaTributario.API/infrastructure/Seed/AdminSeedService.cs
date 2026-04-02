using MapaTributario.API.Domain.Entities;
using MapaTributario.API.Domain.Interfaces;

namespace MapaTributario.API.Infrastructure.Seed;

public class AdminSeedService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ILogger<AdminSeedService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        const string adminEmail = "admin@admin.com";

        var existente = await _userRepository.GetByEmailAsync(adminEmail);
        if (existente is not null)
        {
            _logger.LogInformation("Usuário admin já existe. Seed ignorado.");
            return;
        }

        string hash = _passwordHasher.Hash("12345678");
        var admin = User.Create(adminEmail, "Administrador", hash, "Admin");

        await _userRepository.CreateAsync(admin);
        _logger.LogInformation("Seed de usuário admin concluído: {Email} criado com role Admin.", adminEmail);
    }
}

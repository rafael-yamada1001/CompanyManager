using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger  = logger;
    }

    public async Task SeedAsync()
    {
        // Aplica migrações pendentes (cria o banco se não existir, atualiza schema sem perder dados)
        await _context.Database.MigrateAsync();

        if (await _context.Users.AnyAsync())
            return;

        var rafaelyamada = new User(
            Guid.NewGuid(),
            "rafaelyamada@company.com",
            BCrypt.Net.BCrypt.HashPassword("Rafa@123"),
            role: "admin"
        );

        var admin = new User(
            Guid.NewGuid(),
            "admin@company.com",
            BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            role: "admin"
        );

        var user = new User(
            Guid.NewGuid(),
            "user@company.com",
            BCrypt.Net.BCrypt.HashPassword("User@123"),
            role: "user"
        );

        _context.Users.AddRange(rafaelyamada, admin, user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seed concluído: 3 usuários padrão criados.");
    }
}

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
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // EnsureCreatedAsync cria o schema direto do modelo EF Core sem precisar de migrations
        await _context.Database.EnsureCreatedAsync();

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

        _logger.LogInformation(
            "Seed concluído: " +
            "rafaelyamada@company.com / Rafa@123 (admin) | " +
            "admin@company.com / Admin@123 (admin) | " +
            "user@company.com / User@123 (user)"
        );
    }
}

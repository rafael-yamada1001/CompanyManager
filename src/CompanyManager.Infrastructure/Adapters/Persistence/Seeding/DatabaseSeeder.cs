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

    /// <summary>ID fixo do técnico sistema "A Definir" — usado quando o técnico ainda não foi escolhido.</summary>
    public static readonly Guid ADefinirTechnicianId = new("00000000-0000-0000-0000-000000000001");

    public async Task SeedAsync()
    {
        await EnsureMigrationsReadyAsync();
        await EnsureADefinirTechnicianAsync();

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

    /// <summary>
    /// Garante que o técnico sistema "A Definir" existe com ID fixo.
    /// Esse técnico é usado quando um agendamento precisa de um técnico ainda não definido.
    /// </summary>
    private async Task EnsureADefinirTechnicianAsync()
    {
        var exists = await _context.Technicians.AnyAsync(t => t.Id == ADefinirTechnicianId);
        if (!exists)
        {
            var aDefinir = new Technician(ADefinirTechnicianId, "A Definir", null, null);
            _context.Technicians.Add(aDefinir);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Técnico sistema 'A Definir' criado.");
        }
    }

    // ── Schema ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Garante que o banco está pronto para receber migrações EF Core.
    /// Se o banco foi criado manualmente (EnsureCreated, sem histórico de migrações),
    /// apaga e recria via MigrateAsync para que o EF assuma o controle.
    /// A partir daí, futuras migrações atualizam sem apagar dados.
    /// </summary>
    private async Task EnsureMigrationsReadyAsync()
    {
        // Banco não existe → cria normalmente via migrations
        if (!await _context.Database.CanConnectAsync())
        {
            _logger.LogInformation("Banco não encontrado — criando via migrations...");
            await _context.Database.MigrateAsync();
            return;
        }

        // Banco existe mas foi criado sem migrations (não tem __EFMigrationsHistory)
        // Isso ocorre apenas na transição de EnsureCreated → MigrateAsync
        if (!await MigrationHistoryExistsAsync())
        {
            _logger.LogWarning(
                "Banco existente sem histórico de migrações detectado. " +
                "Recriando para que o EF Core assuma o controle do schema...");
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Banco recriado com sucesso. Migrações futuras não apagarão dados.");
            return;
        }

        // Banco gerenciado pelo EF → aplica apenas migrações pendentes (sem perda de dados)
        await _context.Database.MigrateAsync();
    }

    /// <summary>
    /// Verifica se a tabela de histórico do EF Core existe no banco.
    /// Se não existir, o banco foi criado fora do controle de migrações.
    /// </summary>
    private async Task<bool> MigrationHistoryExistsAsync()
    {
        var conn = _context.Database.GetDbConnection();
        await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            await conn.CloseAsync();
        }
    }
}

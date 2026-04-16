using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CompanyManager.Infrastructure.Adapters.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>                     Users                     => Set<User>();
    public DbSet<Department>               Departments               => Set<Department>();
    public DbSet<Technician>               Technicians               => Set<Technician>();
    public DbSet<TechnicianSchedule>       TechnicianSchedules       => Set<TechnicianSchedule>();
    public DbSet<Item>                     Items                     => Set<Item>();
    public DbSet<UserDepartmentPermission> UserDepartmentPermissions => Set<UserDepartmentPermission>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── User ───────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("user");
            e.Property(u => u.IsBlocked).HasDefaultValue(false);
            e.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
            e.Property(u => u.HasTechnicianAccess).HasDefaultValue(false);
            e.Property(u => u.LastLoginAt).IsRequired(false);
        });

        // ── Department ─────────────────────────────────────────
        mb.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(150);
            e.Property(d => d.Description).HasMaxLength(500);
            e.Property(d => d.CreatedAt).IsRequired();
        });

        // ── Technician ─────────────────────────────────────────
        mb.Entity<Technician>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(150);
            e.Property(t => t.Phone).HasMaxLength(30);
            e.Property(t => t.Region).HasMaxLength(100);
            e.Property(t => t.IsFullTime).HasDefaultValue(false);
            e.Property(t => t.CreatedAt).IsRequired();
        });

        // ── TechnicianSchedule ─────────────────────────────────
        mb.Entity<TechnicianSchedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.TechnicianId).IsRequired();
            e.Property(s => s.Date).IsRequired();
            e.Property(s => s.Title).IsRequired().HasMaxLength(200);
            e.Property(s => s.Client).HasMaxLength(200);
            e.Property(s => s.Notes).HasMaxLength(500);
            e.Property(s => s.Status).IsRequired().HasMaxLength(30).HasDefaultValue("confirmado");
            e.Property(s => s.CreatedAt).IsRequired();
            e.HasIndex(s => s.TechnicianId);
        });

        // ── Item ───────────────────────────────────────────────
        mb.Entity<Item>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.DepartmentId).IsRequired();
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
            e.Property(i => i.Serial).HasMaxLength(100);
            e.Property(i => i.Category).IsRequired().HasMaxLength(100);
            e.Property(i => i.Location)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(ItemLocation.Estoque);
            e.Property(i => i.Observations).HasMaxLength(500);
            e.Property(i => i.CreatedAt).IsRequired();
            e.HasIndex(i => i.DepartmentId);
        });

        // ── UserDepartmentPermission ───────────────────────────
        mb.Entity<UserDepartmentPermission>(e =>
        {
            e.HasKey(p => new { p.UserId, p.DepartmentId });
            e.Property(p => p.Level)
                .IsRequired()
                .HasConversion<string>();
        });

        // ── Garante que todos os DateTime lidos do SQLite voltem como UTC ──
        var utcConverter  = new ValueConverter<DateTime,  DateTime> (v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcConverterN = new ValueConverter<DateTime?, DateTime?>(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entity in mb.Model.GetEntityTypes())
            foreach (var prop in entity.GetProperties())
            {
                if (prop.ClrType == typeof(DateTime))  prop.SetValueConverter(utcConverter);
                if (prop.ClrType == typeof(DateTime?)) prop.SetValueConverter(utcConverterN);
            }
    }
}

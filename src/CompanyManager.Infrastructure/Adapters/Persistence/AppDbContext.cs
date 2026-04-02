using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>                     Users                     => Set<User>();
    public DbSet<Department>               Departments               => Set<Department>();
    public DbSet<DepartmentPerson>         DepartmentPeople          => Set<DepartmentPerson>();
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
        });

        // ── Department ─────────────────────────────────────────
        mb.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(150);
            e.Property(d => d.Description).HasMaxLength(500);
            e.Property(d => d.CreatedAt).IsRequired();
        });

        // ── DepartmentPerson ───────────────────────────────────
        mb.Entity<DepartmentPerson>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.DepartmentId).IsRequired();
            e.Property(p => p.Name).IsRequired().HasMaxLength(150);
            e.Property(p => p.CreatedAt).IsRequired();
            e.HasIndex(p => p.DepartmentId);
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
    }
}

using CompanyManager.Application.Ports.Input;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Application.Services;
using CompanyManager.Infrastructure.Adapters.Persistence;
using CompanyManager.Infrastructure.Adapters.Persistence.Repositories;
using CompanyManager.Infrastructure.Adapters.Persistence.Seeding;
using CompanyManager.Infrastructure.Adapters.Security;
using CompanyManager.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(options =>
            configuration.GetSection("Jwt").Bind(options));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();
        services.AddScoped<ITechnicianScheduleRepository, TechnicianScheduleRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IEngineeringProjectRepository, EngineeringProjectRepository>();
        services.AddScoped<IProjectDocumentRepository, ProjectDocumentRepository>();

        // Security
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        // Application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<DepartmentService>();
        services.AddScoped<ItemService>();
        services.AddScoped<UserService>();
        services.AddScoped<TechnicianService>();
        services.AddScoped<IEngineeringService, EngineeringService>();

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

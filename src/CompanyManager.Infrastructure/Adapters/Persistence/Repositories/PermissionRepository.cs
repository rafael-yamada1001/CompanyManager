using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AppDbContext _ctx;
    public PermissionRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<UserDepartmentPermission>> GetByUserAsync(Guid userId) =>
        _ctx.UserDepartmentPermissions.Where(p => p.UserId == userId).ToListAsync();

    public Task<UserDepartmentPermission?> GetAsync(Guid userId, Guid departmentId) =>
        _ctx.UserDepartmentPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DepartmentId == departmentId);

    public async Task UpsertAsync(UserDepartmentPermission permission)
    {
        var existing = await GetAsync(permission.UserId, permission.DepartmentId);
        if (existing is null)
            _ctx.UserDepartmentPermissions.Add(permission);
        else
            _ctx.UserDepartmentPermissions.Update(permission);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserDepartmentPermission permission)
    {
        _ctx.UserDepartmentPermissions.Remove(permission);
        await _ctx.SaveChangesAsync();
    }

    public async Task<bool> HasAccessAsync(Guid userId, Guid departmentId, PermissionLevel minLevel)
    {
        var perm = await GetAsync(userId, departmentId);
        return perm is not null && perm.Level >= minLevel;
    }
}

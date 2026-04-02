using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;

namespace CompanyManager.Application.Ports.Output;

public interface IPermissionRepository
{
    Task<List<UserDepartmentPermission>> GetByUserAsync(Guid userId);
    Task<UserDepartmentPermission?> GetAsync(Guid userId, Guid departmentId);
    Task UpsertAsync(UserDepartmentPermission permission);
    Task DeleteAsync(UserDepartmentPermission permission);
    Task<bool> HasAccessAsync(Guid userId, Guid departmentId, PermissionLevel minLevel);
}

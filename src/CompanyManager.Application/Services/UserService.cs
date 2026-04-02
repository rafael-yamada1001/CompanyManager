using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class UserService
{
    private readonly IUserRepository       _users;
    private readonly IPasswordHasher       _hasher;
    private readonly IPermissionRepository _permissions;
    private readonly IDepartmentRepository _departments;

    public UserService(IUserRepository users, IPasswordHasher hasher,
        IPermissionRepository permissions, IDepartmentRepository departments)
    {
        _users       = users;
        _hasher      = hasher;
        _permissions = permissions;
        _departments = departments;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        var users = await _users.GetAllAsync();
        var result = new List<UserResponseDto>();
        foreach (var u in users)
            result.Add(await BuildDtoAsync(u));
        return result;
    }

    public async Task<UserResponseDto> GetByIdAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.", "user_not_found");
        return await BuildDtoAsync(user);
    }

    public async Task<List<UserPermissionDto>> GetMyPermissionsAsync(Guid userId)
    {
        var perms = await _permissions.GetByUserAsync(userId);
        var result = new List<UserPermissionDto>();
        foreach (var p in perms)
        {
            var dep = await _departments.GetByIdAsync(p.DepartmentId);
            if (dep is not null)
                result.Add(new UserPermissionDto(dep.Id, dep.Name, p.Level.ToString()));
        }
        return result;
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        var existing = await _users.FindByEmailAsync(dto.Email);
        if (existing is not null)
            throw new DomainException("E-mail já cadastrado.", "email_in_use");

        var user = new User(Guid.NewGuid(), dto.Email, _hasher.Hash(dto.Password), dto.Role.ToLowerInvariant());
        await _users.AddAsync(user);
        return await BuildDtoAsync(user);
    }

    public async Task<UserResponseDto> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.", "user_not_found");

        user.UpdateProfile(
            dto.Role?.ToLowerInvariant(),
            dto.Password is not null ? _hasher.Hash(dto.Password) : null
        );
        await _users.UpdateAsync(user);
        return await BuildDtoAsync(user);
    }

    public async Task UnblockAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.", "user_not_found");
        user.Unblock();
        await _users.UpdateAsync(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.", "user_not_found");
        await _users.DeleteAsync(user);
    }

    public async Task SetPermissionAsync(Guid userId, SetPermissionDto dto)
    {
        _ = await _users.GetByIdAsync(userId)
            ?? throw new DomainException("Usuário não encontrado.", "user_not_found");
        _ = await _departments.GetByIdAsync(dto.DepartmentId)
            ?? throw new DomainException("Departamento não encontrado.", "department_not_found");

        if (!Enum.TryParse<PermissionLevel>(dto.Level, ignoreCase: true, out var level))
            throw new DomainException("Nível de permissão inválido. Use: Visualizar, Editar ou Gerenciar.", "invalid_permission_level");

        var existing = await _permissions.GetAsync(userId, dto.DepartmentId);
        if (existing is null)
            await _permissions.UpsertAsync(new UserDepartmentPermission(userId, dto.DepartmentId, level));
        else
        {
            existing.UpdateLevel(level);
            await _permissions.UpsertAsync(existing);
        }
    }

    public async Task RemovePermissionAsync(Guid userId, Guid departmentId)
    {
        var perm = await _permissions.GetAsync(userId, departmentId)
            ?? throw new DomainException("Permissão não encontrada.", "permission_not_found");
        await _permissions.DeleteAsync(perm);
    }

    // ── Helper ─────────────────────────────────────────────────
    private async Task<UserResponseDto> BuildDtoAsync(User user)
    {
        var perms = await _permissions.GetByUserAsync(user.Id);
        var permDtos = new List<UserPermissionDto>();
        foreach (var p in perms)
        {
            var dep = await _departments.GetByIdAsync(p.DepartmentId);
            if (dep is not null)
                permDtos.Add(new UserPermissionDto(dep.Id, dep.Name, p.Level.ToString()));
        }
        return new UserResponseDto(user.Id, user.Email, user.Role, user.IsBlocked, user.FailedLoginAttempts, permDtos);
    }
}

using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateUserDto(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    string Role = "user"
);

public record UpdateUserDto(
    string? Password,
    string? Role
);

public record UserResponseDto(
    Guid Id,
    string Email,
    string Role,
    bool IsBlocked,
    int FailedLoginAttempts,
    bool HasTechnicianAccess,
    List<UserPermissionDto> Permissions
);

public record UserPermissionDto(
    Guid DepartmentId,
    string DepartmentName,
    string Level          // "Visualizar" | "Editar" | "Gerenciar"
);

public record SetPermissionDto(
    [Required] Guid DepartmentId,
    [Required] string Level       // "Visualizar" | "Editar" | "Gerenciar"
);

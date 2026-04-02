using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateDepartmentDto(
    [Required] string Name,
    string? Description
);

public record UpdateDepartmentDto(
    [Required] string Name,
    string? Description
);

public record DepartmentResponseDto(
    Guid     Id,
    string   Name,
    string?  Description,
    int      ItemCount,
    int      EstoqueCount,
    int      CampoCount,
    int      ManutencaoCount,
    DateTime CreatedAt
);

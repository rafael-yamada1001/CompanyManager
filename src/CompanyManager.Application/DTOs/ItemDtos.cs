using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateItemDto(
    [Required] string Name,
    string? Serial,
    [Required] string Category,
    string? Observations
);

public record UpdateItemDto(
    [Required] string Name,
    string? Serial,
    [Required] string Category,
    string? Observations
);

public record MoveItemDto(
    [Required] string Location,   // "estoque" | "campo" | "manutencao"
    Guid? PersonId,
    string? Observations
);

public record ItemResponseDto(
    Guid Id,
    Guid DepartmentId,
    string Name,
    string? Serial,
    string Category,
    string Location,
    Guid? PersonId,
    string? PersonName,
    string? Observations,
    DateTime CreatedAt
);

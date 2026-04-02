using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreatePersonDto([Required] string Name);

public record PersonResponseDto(
    Guid Id,
    Guid DepartmentId,
    string Name,
    int ItemsWithPerson,
    DateTime CreatedAt
);

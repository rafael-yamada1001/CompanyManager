using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateTechnicianDto(
    [Required] string Name,
    string? Phone,
    string? Region
);

public record TechnicianResponseDto(
    Guid Id,
    Guid DepartmentId,
    string Name,
    string? Phone,
    string? Region,
    int ItemsWithTechnician,
    DateTime CreatedAt
);

public record CreateTechnicianScheduleDto(
    [Required] DateTime Date,
    [Required] string Title,
    string? Notes
);

public record UpdateTechnicianScheduleDto(
    [Required] DateTime Date,
    [Required] string Title,
    string? Notes
);

public record TechnicianScheduleResponseDto(
    Guid Id,
    Guid TechnicianId,
    string TechnicianName,
    DateTime Date,
    string Title,
    string? Notes,
    DateTime CreatedAt
);

using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateTechnicianDto(
    [Required] string Name,
    string? Phone,
    string? Region
);

public record UpdateTechnicianDto(
    [Required] string Name,
    string? Phone,
    string? Region
);

public record TechnicianResponseDto(
    Guid     Id,
    string   Name,
    string?  Phone,
    string?  Region,
    int      ItemsWithTechnician,
    DateTime CreatedAt
);

public record CreateTechnicianScheduleDto(
    DateTime Date,
    [Required] string Title,
    string? Client,
    string? Notes,
    string Status = "confirmado"
);

public record UpdateTechnicianScheduleDto(
    DateTime Date,
    [Required] string Title,
    string? Client,
    string? Notes,
    string Status = "confirmado"
);

public record TechnicianScheduleResponseDto(
    Guid     Id,
    Guid     TechnicianId,
    string   TechnicianName,
    DateTime Date,
    string   Title,
    string?  Client,
    string?  Notes,
    string   Status,
    DateTime CreatedAt
);

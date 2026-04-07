using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record CreateEngineeringProjectDto(
    [Required] string ProjectNumber,
    [Required] string Name,
    [Required] string Client,
    string? Description,
    string Status,
    [Required] string Responsible,
    DateTime? Deadline
);

public record UpdateEngineeringProjectDto(
    [Required] string ProjectNumber,
    [Required] string Name,
    [Required] string Client,
    string? Description,
    string Status,
    [Required] string Responsible,
    DateTime? Deadline
);

public record EngineeringProjectResponseDto(
    Guid      Id,
    string    ProjectNumber,
    string    Name,
    string    Client,
    string?   Description,
    string    Status,
    string    Responsible,
    DateTime? Deadline,
    int       DocumentCount,
    DateTime  CreatedAt,
    DateTime  UpdatedAt
);

public record CreateProjectDocumentDto(
    [Required] string FileName,
    [Required] string FilePath,
    string? Revision,
    string? Description,
    string? AddedBy
);

public record UpdateProjectDocumentDto(
    [Required] string FileName,
    [Required] string FilePath,
    string? Revision,
    string? Description,
    string? AddedBy
);

public record ProjectDocumentResponseDto(
    Guid     Id,
    Guid     ProjectId,
    string   FileName,
    string   FilePath,
    string?  Revision,
    string?  Description,
    string?  FileType,
    string?  AddedBy,
    DateTime CreatedAt
);

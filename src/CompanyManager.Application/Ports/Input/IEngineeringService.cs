using CompanyManager.Application.DTOs;

namespace CompanyManager.Application.Ports.Input;

public interface IEngineeringService
{
    Task<List<EngineeringProjectResponseDto>> GetAllAsync(string? query, string? status, string? client);
    Task<EngineeringProjectResponseDto> GetByIdAsync(Guid id);
    Task<EngineeringProjectResponseDto> CreateAsync(CreateEngineeringProjectDto dto);
    Task<EngineeringProjectResponseDto> UpdateAsync(Guid id, UpdateEngineeringProjectDto dto);
    Task DeleteAsync(Guid id);

    Task<List<ProjectDocumentResponseDto>> GetDocumentsAsync(Guid projectId);
    Task<ProjectDocumentResponseDto> AddDocumentAsync(Guid projectId, CreateProjectDocumentDto dto);
    Task<ProjectDocumentResponseDto> UpdateDocumentAsync(Guid docId, UpdateProjectDocumentDto dto);
    Task DeleteDocumentAsync(Guid docId);
}

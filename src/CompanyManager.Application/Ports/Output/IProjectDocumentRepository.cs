using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface IProjectDocumentRepository
{
    Task<List<ProjectDocument>> GetByProjectAsync(Guid projectId);
    Task<ProjectDocument?> GetByIdAsync(Guid id);
    Task AddAsync(ProjectDocument doc);
    Task UpdateAsync(ProjectDocument doc);
    Task DeleteAsync(Guid id);
    Task DeleteByProjectAsync(Guid projectId);
}

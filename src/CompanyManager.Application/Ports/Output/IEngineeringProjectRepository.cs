using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface IEngineeringProjectRepository
{
    Task<List<EngineeringProject>> GetAllAsync();
    Task<EngineeringProject?> GetByIdAsync(Guid id);
    Task<List<EngineeringProject>> SearchAsync(string? query, string? status, string? client);
    Task AddAsync(EngineeringProject project);
    Task UpdateAsync(EngineeringProject project);
    Task DeleteAsync(Guid id);
}

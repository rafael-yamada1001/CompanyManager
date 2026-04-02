using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(Guid id);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(Department department);
}

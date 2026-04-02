using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface IPersonRepository
{
    Task<List<DepartmentPerson>> GetByDepartmentAsync(Guid departmentId);
    Task<DepartmentPerson?> GetByIdAsync(Guid id);
    Task AddAsync(DepartmentPerson person);
    Task UpdateAsync(DepartmentPerson person);
    Task DeleteAsync(DepartmentPerson person);
}

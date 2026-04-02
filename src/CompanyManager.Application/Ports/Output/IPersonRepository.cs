using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface ITechnicianRepository
{
    Task<List<Technician>> GetAllAsync();
    Task<Technician?> GetByIdAsync(Guid id);
    Task AddAsync(Technician technician);
    Task UpdateAsync(Technician technician);
    Task DeleteAsync(Technician technician);
}

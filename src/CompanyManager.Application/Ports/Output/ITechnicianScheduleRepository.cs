using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface ITechnicianScheduleRepository
{
    Task<List<TechnicianSchedule>> GetByTechnicianAsync(Guid technicianId);
    Task<TechnicianSchedule?> GetByIdAsync(Guid id);
    Task AddAsync(TechnicianSchedule schedule);
    Task UpdateAsync(TechnicianSchedule schedule);
    Task DeleteAsync(TechnicianSchedule schedule);
}

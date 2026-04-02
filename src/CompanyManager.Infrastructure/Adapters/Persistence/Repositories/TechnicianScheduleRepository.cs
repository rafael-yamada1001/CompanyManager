using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class TechnicianScheduleRepository : ITechnicianScheduleRepository
{
    private readonly AppDbContext _ctx;
    public TechnicianScheduleRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<TechnicianSchedule>> GetByTechnicianAsync(Guid technicianId) =>
        _ctx.TechnicianSchedules
            .Where(s => s.TechnicianId == technicianId)
            .OrderBy(s => s.Date)
            .ToListAsync();

    public Task<TechnicianSchedule?> GetByIdAsync(Guid id) =>
        _ctx.TechnicianSchedules.FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(TechnicianSchedule schedule)
    {
        _ctx.TechnicianSchedules.Add(schedule);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(TechnicianSchedule schedule)
    {
        _ctx.TechnicianSchedules.Update(schedule);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(TechnicianSchedule schedule)
    {
        _ctx.TechnicianSchedules.Remove(schedule);
        await _ctx.SaveChangesAsync();
    }
}

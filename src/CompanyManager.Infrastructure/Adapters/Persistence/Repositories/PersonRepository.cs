using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class TechnicianRepository : ITechnicianRepository
{
    private readonly AppDbContext _ctx;
    public TechnicianRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<Technician>> GetAllAsync() =>
        _ctx.Technicians.OrderBy(t => t.Name).ToListAsync();

    public Task<List<Technician>> GetByDepartmentAsync(Guid departmentId) =>
        _ctx.Technicians
            .Where(t => t.DepartmentId == departmentId)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public Task<Technician?> GetByIdAsync(Guid id) =>
        _ctx.Technicians.FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(Technician technician)
    {
        _ctx.Technicians.Add(technician);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(Technician technician)
    {
        _ctx.Technicians.Update(technician);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Technician technician)
    {
        _ctx.Technicians.Remove(technician);
        await _ctx.SaveChangesAsync();
    }
}

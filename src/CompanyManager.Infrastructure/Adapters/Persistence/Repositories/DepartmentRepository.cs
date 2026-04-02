using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _ctx;
    public DepartmentRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<Department>> GetAllAsync() =>
        _ctx.Departments.OrderBy(d => d.Name).ToListAsync();

    public Task<Department?> GetByIdAsync(Guid id) =>
        _ctx.Departments.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Department department)
    {
        _ctx.Departments.Add(department);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _ctx.Departments.Update(department);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Department department)
    {
        _ctx.Departments.Remove(department);
        await _ctx.SaveChangesAsync();
    }
}

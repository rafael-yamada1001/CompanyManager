using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly AppDbContext _ctx;
    public PersonRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<DepartmentPerson>> GetByDepartmentAsync(Guid departmentId) =>
        _ctx.DepartmentPeople
            .Where(p => p.DepartmentId == departmentId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public Task<DepartmentPerson?> GetByIdAsync(Guid id) =>
        _ctx.DepartmentPeople.FirstOrDefaultAsync(p => p.Id == id);

    public async Task AddAsync(DepartmentPerson person)
    {
        _ctx.DepartmentPeople.Add(person);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(DepartmentPerson person)
    {
        _ctx.DepartmentPeople.Update(person);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(DepartmentPerson person)
    {
        _ctx.DepartmentPeople.Remove(person);
        await _ctx.SaveChangesAsync();
    }
}

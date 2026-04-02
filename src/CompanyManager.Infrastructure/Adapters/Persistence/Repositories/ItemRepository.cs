using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _ctx;
    public ItemRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<Item>> GetAllAsync() =>
        _ctx.Items.OrderBy(i => i.Name).ToListAsync();

    public Task<List<Item>> GetByDepartmentAsync(Guid departmentId) =>
        _ctx.Items
            .Where(i => i.DepartmentId == departmentId)
            .OrderBy(i => i.Name)
            .ToListAsync();

    public Task<Item?> GetByIdAsync(Guid id) =>
        _ctx.Items.FirstOrDefaultAsync(i => i.Id == id);

    public async Task AddAsync(Item item)
    {
        _ctx.Items.Add(item);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(Item item)
    {
        _ctx.Items.Update(item);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Item item)
    {
        _ctx.Items.Remove(item);
        await _ctx.SaveChangesAsync();
    }
}

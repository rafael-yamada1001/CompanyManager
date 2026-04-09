using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface IItemRepository
{
    Task<List<Item>> GetAllAsync();
    Task<List<Item>> GetByDepartmentAsync(Guid departmentId);
    Task<Item?> GetByIdAsync(Guid id);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
    Task DeleteAsync(Item item);
}

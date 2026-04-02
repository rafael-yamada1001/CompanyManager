using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _ctx;
    public UserRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<User>> GetAllAsync() =>
        _ctx.Users.OrderBy(u => u.Email).ToListAsync();

    public Task<User?> GetByIdAsync(Guid id) =>
        _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> FindByEmailAsync(string email) =>
        _ctx.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task AddAsync(User user)
    {
        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _ctx.Users.Update(user);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _ctx.Users.Remove(user);
        await _ctx.SaveChangesAsync();
    }
}

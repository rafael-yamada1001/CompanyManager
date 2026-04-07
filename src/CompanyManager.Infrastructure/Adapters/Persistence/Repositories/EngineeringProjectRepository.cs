using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class EngineeringProjectRepository : IEngineeringProjectRepository
{
    private readonly AppDbContext _ctx;
    public EngineeringProjectRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<EngineeringProject>> GetAllAsync() =>
        _ctx.EngineeringProjects.OrderByDescending(p => p.CreatedAt).ToListAsync();

    public Task<EngineeringProject?> GetByIdAsync(Guid id) =>
        _ctx.EngineeringProjects.FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<EngineeringProject>> SearchAsync(string? query, string? status, string? client)
    {
        var q = _ctx.EngineeringProjects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLowerInvariant();
            q = q.Where(p =>
                p.ProjectNumber.ToLower().Contains(lower) ||
                p.Name.ToLower().Contains(lower) ||
                p.Client.ToLower().Contains(lower) ||
                p.Responsible.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(client))
            q = q.Where(p => p.Client.ToLower().Contains(client.ToLowerInvariant()));

        return q.OrderByDescending(p => p.UpdatedAt).ToListAsync();
    }

    public async Task AddAsync(EngineeringProject project)
    {
        _ctx.EngineeringProjects.Add(project);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(EngineeringProject project)
    {
        _ctx.EngineeringProjects.Update(project);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _ctx.EngineeringProjects.FindAsync(id);
        if (project is null) return;
        _ctx.EngineeringProjects.Remove(project);
        await _ctx.SaveChangesAsync();
    }
}

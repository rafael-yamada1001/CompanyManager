using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyManager.Infrastructure.Adapters.Persistence.Repositories;

public class ProjectDocumentRepository : IProjectDocumentRepository
{
    private readonly AppDbContext _ctx;
    public ProjectDocumentRepository(AppDbContext ctx) => _ctx = ctx;

    public Task<List<ProjectDocument>> GetByProjectAsync(Guid projectId) =>
        _ctx.ProjectDocuments
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public Task<ProjectDocument?> GetByIdAsync(Guid id) =>
        _ctx.ProjectDocuments.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(ProjectDocument doc)
    {
        _ctx.ProjectDocuments.Add(doc);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectDocument doc)
    {
        _ctx.ProjectDocuments.Update(doc);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await _ctx.ProjectDocuments.FindAsync(id);
        if (doc is null) return;
        _ctx.ProjectDocuments.Remove(doc);
        await _ctx.SaveChangesAsync();
    }

    public async Task DeleteByProjectAsync(Guid projectId)
    {
        var docs = await _ctx.ProjectDocuments
            .Where(d => d.ProjectId == projectId)
            .ToListAsync();

        if (docs.Count > 0)
        {
            _ctx.ProjectDocuments.RemoveRange(docs);
            await _ctx.SaveChangesAsync();
        }
    }
}

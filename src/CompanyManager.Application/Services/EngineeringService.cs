using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Input;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class EngineeringService : IEngineeringService
{
    private readonly IEngineeringProjectRepository _projects;
    private readonly IProjectDocumentRepository    _documents;

    public EngineeringService(
        IEngineeringProjectRepository projects,
        IProjectDocumentRepository    documents)
    {
        _projects  = projects;
        _documents = documents;
    }

    // ── Projetos ───────────────────────────────────────────────
    public async Task<List<EngineeringProjectResponseDto>> GetAllAsync(string? query, string? status, string? client)
    {
        var projects = await _projects.SearchAsync(query, status, client);
        var result   = new List<EngineeringProjectResponseDto>(projects.Count);

        foreach (var p in projects)
        {
            var docs = await _documents.GetByProjectAsync(p.Id);
            result.Add(ToDto(p, docs.Count));
        }

        return result;
    }

    public async Task<EngineeringProjectResponseDto> GetByIdAsync(Guid id)
    {
        var project = await _projects.GetByIdAsync(id)
            ?? throw new BusinessException("Projeto não encontrado.", "project_not_found");

        var docs = await _documents.GetByProjectAsync(id);
        return ToDto(project, docs.Count);
    }

    public async Task<EngineeringProjectResponseDto> CreateAsync(CreateEngineeringProjectDto dto)
    {
        var project = new EngineeringProject(
            Guid.NewGuid(),
            dto.ProjectNumber,
            dto.Name,
            dto.Client,
            dto.Description,
            dto.Status ?? "em_andamento",
            dto.Responsible,
            dto.Deadline);

        await _projects.AddAsync(project);
        return ToDto(project, 0);
    }

    public async Task<EngineeringProjectResponseDto> UpdateAsync(Guid id, UpdateEngineeringProjectDto dto)
    {
        var project = await _projects.GetByIdAsync(id)
            ?? throw new BusinessException("Projeto não encontrado.", "project_not_found");

        project.Update(
            dto.ProjectNumber,
            dto.Name,
            dto.Client,
            dto.Description,
            dto.Status ?? "em_andamento",
            dto.Responsible,
            dto.Deadline);

        await _projects.UpdateAsync(project);

        var docs = await _documents.GetByProjectAsync(id);
        return ToDto(project, docs.Count);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _projects.GetByIdAsync(id)
            ?? throw new BusinessException("Projeto não encontrado.", "project_not_found");

        await _documents.DeleteByProjectAsync(id);
        await _projects.DeleteAsync(project.Id);
    }

    // ── Documentos ─────────────────────────────────────────────
    public async Task<List<ProjectDocumentResponseDto>> GetDocumentsAsync(Guid projectId)
    {
        _ = await _projects.GetByIdAsync(projectId)
            ?? throw new BusinessException("Projeto não encontrado.", "project_not_found");

        var docs = await _documents.GetByProjectAsync(projectId);
        return docs.Select(ToDocDto).ToList();
    }

    public async Task<ProjectDocumentResponseDto> AddDocumentAsync(Guid projectId, CreateProjectDocumentDto dto)
    {
        _ = await _projects.GetByIdAsync(projectId)
            ?? throw new BusinessException("Projeto não encontrado.", "project_not_found");

        var doc = new ProjectDocument(
            Guid.NewGuid(),
            projectId,
            dto.FileName,
            dto.FilePath,
            dto.Revision,
            dto.Description,
            dto.AddedBy);

        await _documents.AddAsync(doc);
        return ToDocDto(doc);
    }

    public async Task<ProjectDocumentResponseDto> UpdateDocumentAsync(Guid docId, UpdateProjectDocumentDto dto)
    {
        var doc = await _documents.GetByIdAsync(docId)
            ?? throw new BusinessException("Documento não encontrado.", "document_not_found");

        doc.Update(dto.FileName, dto.FilePath, dto.Revision, dto.Description);
        await _documents.UpdateAsync(doc);
        return ToDocDto(doc);
    }

    public async Task DeleteDocumentAsync(Guid docId)
    {
        var doc = await _documents.GetByIdAsync(docId)
            ?? throw new BusinessException("Documento não encontrado.", "document_not_found");

        await _documents.DeleteAsync(doc.Id);
    }

    // ── Helpers ────────────────────────────────────────────────
    private static EngineeringProjectResponseDto ToDto(EngineeringProject p, int docCount) =>
        new(p.Id, p.ProjectNumber, p.Name, p.Client, p.Description,
            p.Status, p.Responsible, p.Deadline, docCount, p.CreatedAt, p.UpdatedAt);

    private static ProjectDocumentResponseDto ToDocDto(ProjectDocument d) =>
        new(d.Id, d.ProjectId, d.FileName, d.FilePath, d.Revision, d.Description, d.FileType, d.AddedBy, d.CreatedAt);
}

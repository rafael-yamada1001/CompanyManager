namespace CompanyManager.Domain.Entities;

public class ProjectDocument
{
    public Guid    Id          { get; private set; }
    public Guid    ProjectId   { get; private set; }
    public string  FileName    { get; private set; } = null!;
    public string  FilePath    { get; private set; } = null!;   // UNC path or local path on file server
    public string? Revision    { get; private set; }            // e.g. "Rev A", "00", "01"
    public string? Description { get; private set; }
    public string? FileType    { get; private set; }            // .dwg, .pdf, .step, .dxf, etc.
    public string? AddedBy     { get; private set; }
    public DateTime CreatedAt  { get; private set; }

    private ProjectDocument() { }

    public ProjectDocument(Guid id, Guid projectId, string fileName, string filePath, string? revision, string? description, string? addedBy)
    {
        Id          = id;
        ProjectId   = projectId;
        FileName    = fileName.Trim();
        FilePath    = filePath.Trim();
        Revision    = revision?.Trim();
        Description = description?.Trim();
        FileType    = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
        AddedBy     = addedBy?.Trim();
        CreatedAt   = DateTime.UtcNow;
    }

    public void Update(string fileName, string filePath, string? revision, string? description)
    {
        FileName    = fileName.Trim();
        FilePath    = filePath.Trim();
        Revision    = revision?.Trim();
        Description = description?.Trim();
        FileType    = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
    }
}

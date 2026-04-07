namespace CompanyManager.Domain.Entities;

public class EngineeringProject
{
    public Guid     Id            { get; private set; }
    public string   ProjectNumber { get; private set; } = null!;  // e.g. "ENG-2024-001"
    public string   Name          { get; private set; } = null!;
    public string   Client        { get; private set; } = null!;
    public string?  Description   { get; private set; }
    public string   Status        { get; private set; } = null!;  // "em_andamento","concluido","pausado","cancelado","em_revisao"
    public string   Responsible   { get; private set; } = null!;  // name of engineer
    public DateTime? Deadline      { get; private set; }
    public DateTime CreatedAt     { get; private set; }
    public DateTime UpdatedAt     { get; private set; }

    private EngineeringProject() { }  // EF Core

    public EngineeringProject(Guid id, string projectNumber, string name, string client, string? description, string status, string responsible, DateTime? deadline)
    {
        Id            = id;
        ProjectNumber = projectNumber.Trim().ToUpperInvariant();
        Name          = name.Trim();
        Client        = client.Trim();
        Description   = description?.Trim();
        Status        = status;
        Responsible   = responsible.Trim();
        Deadline      = deadline;
        CreatedAt     = DateTime.UtcNow;
        UpdatedAt     = DateTime.UtcNow;
    }

    public void Update(string projectNumber, string name, string client, string? description, string status, string responsible, DateTime? deadline)
    {
        ProjectNumber = projectNumber.Trim().ToUpperInvariant();
        Name          = name.Trim();
        Client        = client.Trim();
        Description   = description?.Trim();
        Status        = status;
        Responsible   = responsible.Trim();
        Deadline      = deadline;
        UpdatedAt     = DateTime.UtcNow;
    }
}

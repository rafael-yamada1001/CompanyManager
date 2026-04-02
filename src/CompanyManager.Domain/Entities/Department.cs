namespace CompanyManager.Domain.Entities;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Department() { }

    public Department(Guid id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}

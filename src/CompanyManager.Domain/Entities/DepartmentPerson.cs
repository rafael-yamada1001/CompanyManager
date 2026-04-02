namespace CompanyManager.Domain.Entities;

/// <summary>
/// Técnico vinculado a um departamento para rastreamento de itens.
/// Não é um usuário do sistema — representa um técnico de campo.
/// </summary>
public class Technician
{
    public Guid Id { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Region { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Technician() { }

    public Technician(Guid id, Guid departmentId, string name, string? phone, string? region)
    {
        Id = id;
        DepartmentId = departmentId;
        Name = name;
        Phone = phone;
        Region = region;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? phone, string? region)
    {
        Name = name;
        Phone = phone;
        Region = region;
    }
}

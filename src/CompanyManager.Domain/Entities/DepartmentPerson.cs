namespace CompanyManager.Domain.Entities;

/// <summary>
/// Pessoa vinculada a um departamento para rastreamento de itens.
/// Não é um usuário do sistema — apenas um nome para saber com quem está cada item.
/// </summary>
public class DepartmentPerson
{
    public Guid Id { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private DepartmentPerson() { }

    public DepartmentPerson(Guid id, Guid departmentId, string name)
    {
        Id = id;
        DepartmentId = departmentId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name) => Name = name;
}

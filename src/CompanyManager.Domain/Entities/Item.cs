using CompanyManager.Domain.Enums;

namespace CompanyManager.Domain.Entities;

public class Item
{
    public Guid Id { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Serial { get; private set; }
    public string Category { get; private set; } = null!;
    public ItemLocation Location { get; private set; }
    public Guid? PersonId { get; private set; }         // pessoa que está com o item (quando em campo)
    public string? Observations { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Item() { }

    public Item(Guid id, Guid departmentId, string name, string? serial, string category, string? observations)
    {
        Id = id;
        DepartmentId = departmentId;
        Name = name;
        Serial = serial;
        Category = category;
        Observations = observations;
        Location = ItemLocation.Estoque;
        CreatedAt = DateTime.UtcNow;
    }

    public void Move(ItemLocation location, Guid? personId, string? observations)
    {
        Location = location;
        PersonId = location == ItemLocation.Campo ? personId : null;
        if (observations is not null)
            Observations = observations;
    }

    public void Update(string name, string? serial, string category, string? observations)
    {
        Name = name;
        Serial = serial;
        Category = category;
        Observations = observations;
    }
}

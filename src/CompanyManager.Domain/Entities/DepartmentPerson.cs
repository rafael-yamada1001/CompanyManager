namespace CompanyManager.Domain.Entities;

/// <summary>
/// Técnico global — não está vinculado a nenhum departamento específico.
/// Pode retirar itens de qualquer departamento.
/// </summary>
public class Technician
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Region { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Technician() { }

    public Technician(Guid id, string name, string? phone, string? region)
    {
        Id        = id;
        Name      = name;
        Phone     = phone;
        Region    = region;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string? phone, string? region)
    {
        Name   = name;
        Phone  = phone;
        Region = region;
    }
}

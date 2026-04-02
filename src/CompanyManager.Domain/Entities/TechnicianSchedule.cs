namespace CompanyManager.Domain.Entities;

/// <summary>
/// Entrada de agenda de um técnico — indica onde/o quê ele estará em uma data específica.
/// </summary>
public class TechnicianSchedule
{
    public Guid Id { get; private set; }
    public Guid TechnicianId { get; private set; }
    public DateTime Date { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TechnicianSchedule() { }

    public TechnicianSchedule(Guid id, Guid technicianId, DateTime date, string title, string? notes)
    {
        Id = id;
        TechnicianId = technicianId;
        Date = date.Date;
        Title = title;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(DateTime date, string title, string? notes)
    {
        Date = date.Date;
        Title = title;
        Notes = notes;
    }
}

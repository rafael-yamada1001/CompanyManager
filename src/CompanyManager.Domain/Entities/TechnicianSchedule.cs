namespace CompanyManager.Domain.Entities;

/// <summary>
/// Entrada de agenda de um técnico — indica onde/o quê ele estará em uma data específica.
/// </summary>
public class TechnicianSchedule
{
    public Guid Id { get; private set; }
    public Guid TechnicianId { get; private set; }
    public DateTime Date { get; private set; }
    public string Title { get; private set; } = null!;   // serviço / onde estará
    public string? Client { get; private set; }           // cliente (ex: USINA BOM SUCESSO)
    public string? Notes { get; private set; }
    public string Status { get; private set; } = "confirmado"; // confirmado | em_andamento | pendente | a_definir
    public DateTime CreatedAt { get; private set; }

    private TechnicianSchedule() { }

    public TechnicianSchedule(Guid id, Guid technicianId, DateTime date, string title, string? client, string? notes, string status)
    {
        Id           = id;
        TechnicianId = technicianId;
        Date         = date.Date;
        Title        = title;
        Client       = client;
        Notes        = notes;
        Status       = status;
        CreatedAt    = DateTime.UtcNow;
    }

    public void Update(DateTime date, string title, string? client, string? notes, string status)
    {
        Date   = date.Date;
        Title  = title;
        Client = client;
        Notes  = notes;
        Status = status;
    }
}

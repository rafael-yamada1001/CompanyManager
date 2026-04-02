using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class TechnicianService
{
    private readonly ITechnicianRepository         _technicians;
    private readonly ITechnicianScheduleRepository _schedules;
    private readonly IItemRepository               _items;

    public TechnicianService(
        ITechnicianRepository technicians,
        ITechnicianScheduleRepository schedules,
        IItemRepository items)
    {
        _technicians = technicians;
        _schedules   = schedules;
        _items       = items;
    }

    // ── CRUD Técnicos ──────────────────────────────────────────
    public async Task<List<TechnicianResponseDto>> GetAllAsync()
    {
        var techs    = await _technicians.GetAllAsync();
        var allItems = await _items.GetAllAsync();

        return techs.Select(t => new TechnicianResponseDto(
            t.Id, t.Name, t.Phone, t.Region,
            allItems.Count(i => i.PersonId == t.Id),
            t.CreatedAt
        )).ToList();
    }

    public async Task<TechnicianResponseDto> CreateAsync(CreateTechnicianDto dto)
    {
        var tech = new Technician(Guid.NewGuid(), dto.Name.Trim(), dto.Phone?.Trim(), dto.Region?.Trim());
        await _technicians.AddAsync(tech);
        return new TechnicianResponseDto(tech.Id, tech.Name, tech.Phone, tech.Region, 0, tech.CreatedAt);
    }

    public async Task<TechnicianResponseDto> UpdateAsync(Guid id, UpdateTechnicianDto dto)
    {
        var tech = await _technicians.GetByIdAsync(id)
            ?? throw new BusinessException("Técnico não encontrado.", "technician_not_found");

        tech.Update(dto.Name.Trim(), dto.Phone?.Trim(), dto.Region?.Trim());
        await _technicians.UpdateAsync(tech);

        var allItems = await _items.GetAllAsync();
        return new TechnicianResponseDto(tech.Id, tech.Name, tech.Phone, tech.Region,
            allItems.Count(i => i.PersonId == tech.Id), tech.CreatedAt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var tech = await _technicians.GetByIdAsync(id)
            ?? throw new BusinessException("Técnico não encontrado.", "technician_not_found");
        await _technicians.DeleteAsync(tech);
    }

    // ── Agenda ─────────────────────────────────────────────────
    public async Task<List<TechnicianScheduleResponseDto>> GetScheduleAsync(Guid technicianId)
    {
        _ = await _technicians.GetByIdAsync(technicianId)
            ?? throw new BusinessException("Técnico não encontrado.", "technician_not_found");

        var schedules = await _schedules.GetByTechnicianAsync(technicianId);
        var tech      = (await _technicians.GetByIdAsync(technicianId))!;
        return schedules
            .OrderBy(s => s.Date)
            .Select(s => ToDto(s, tech.Name))
            .ToList();
    }

    public async Task<TechnicianScheduleResponseDto> AddScheduleAsync(Guid technicianId, CreateTechnicianScheduleDto dto)
    {
        var tech = await _technicians.GetByIdAsync(technicianId)
            ?? throw new BusinessException("Técnico não encontrado.", "technician_not_found");

        var entry = new TechnicianSchedule(
            Guid.NewGuid(), technicianId,
            dto.Date, dto.Title.Trim(), dto.Client?.Trim(), dto.Notes?.Trim(),
            dto.Status ?? "confirmado");

        await _schedules.AddAsync(entry);
        return ToDto(entry, tech.Name);
    }

    public async Task<TechnicianScheduleResponseDto> UpdateScheduleAsync(Guid scheduleId, UpdateTechnicianScheduleDto dto)
    {
        var entry = await _schedules.GetByIdAsync(scheduleId)
            ?? throw new BusinessException("Entrada de agenda não encontrada.", "schedule_not_found");

        var tech = (await _technicians.GetByIdAsync(entry.TechnicianId))!;

        entry.Update(dto.Date, dto.Title.Trim(), dto.Client?.Trim(), dto.Notes?.Trim(),
            dto.Status ?? "confirmado");
        await _schedules.UpdateAsync(entry);
        return ToDto(entry, tech.Name);
    }

    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        var entry = await _schedules.GetByIdAsync(scheduleId)
            ?? throw new BusinessException("Entrada de agenda não encontrada.", "schedule_not_found");
        await _schedules.DeleteAsync(entry);
    }

    // ── Helper ─────────────────────────────────────────────────
    private static TechnicianScheduleResponseDto ToDto(TechnicianSchedule s, string techName) =>
        new(s.Id, s.TechnicianId, techName, s.Date, s.Title, s.Client, s.Notes, s.Status, s.CreatedAt);
}

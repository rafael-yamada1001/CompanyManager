using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class DepartmentService
{
    private readonly IDepartmentRepository  _departments;
    private readonly ITechnicianRepository  _technicians;
    private readonly IItemRepository        _items;

    public DepartmentService(IDepartmentRepository departments, ITechnicianRepository technicians, IItemRepository items)
    {
        _departments = departments;
        _technicians = technicians;
        _items       = items;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        var deps = await _departments.GetAllAsync();
        var result = new List<DepartmentResponseDto>();

        foreach (var d in deps)
        {
            var depItems = await _items.GetByDepartmentAsync(d.Id);
            var techs    = await _technicians.GetByDepartmentAsync(d.Id);
            result.Add(ToDto(d, depItems, techs));
        }
        return result;
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");

        var depItems = await _items.GetByDepartmentAsync(id);
        var techs    = await _technicians.GetByDepartmentAsync(id);
        return ToDto(dep, depItems, techs);
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var dep = new Department(Guid.NewGuid(), dto.Name.Trim(), dto.Description?.Trim());
        await _departments.AddAsync(dep);
        return ToDto(dep, [], []);
    }

    public async Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");

        dep.Update(dto.Name.Trim(), dto.Description?.Trim());
        await _departments.UpdateAsync(dep);

        var depItems = await _items.GetByDepartmentAsync(id);
        var techs    = await _technicians.GetByDepartmentAsync(id);
        return ToDto(dep, depItems, techs);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");
        await _departments.DeleteAsync(dep);
    }

    // ── Técnicos ───────────────────────────────────────────────
    public async Task<List<TechnicianResponseDto>> GetTechniciansAsync(Guid departmentId)
    {
        var depItems = await _items.GetByDepartmentAsync(departmentId);
        var techs    = await _technicians.GetByDepartmentAsync(departmentId);

        return techs.Select(t => new TechnicianResponseDto(
            t.Id, t.DepartmentId, t.Name, t.Phone, t.Region,
            depItems.Count(i => i.PersonId == t.Id),
            t.CreatedAt
        )).ToList();
    }

    public async Task<TechnicianResponseDto> AddTechnicianAsync(Guid departmentId, CreateTechnicianDto dto)
    {
        _ = await _departments.GetByIdAsync(departmentId)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");

        var tech = new Technician(Guid.NewGuid(), departmentId, dto.Name.Trim(), dto.Phone?.Trim(), dto.Region?.Trim());
        await _technicians.AddAsync(tech);
        return new TechnicianResponseDto(tech.Id, tech.DepartmentId, tech.Name, tech.Phone, tech.Region, 0, tech.CreatedAt);
    }

    public async Task DeleteTechnicianAsync(Guid departmentId, Guid technicianId)
    {
        var tech = await _technicians.GetByIdAsync(technicianId)
            ?? throw new BusinessException("Técnico não encontrado.", "technician_not_found");

        if (tech.DepartmentId != departmentId)
            throw new BusinessException("Técnico não pertence a este departamento.", "technician_not_found");

        await _technicians.DeleteAsync(tech);
    }

    // ── Helper ─────────────────────────────────────────────────
    private static DepartmentResponseDto ToDto(Department d, List<Item> items, List<Technician> techs) =>
        new(d.Id, d.Name, d.Description,
            items.Count,
            items.Count(i => i.Location == ItemLocation.Estoque),
            items.Count(i => i.Location == ItemLocation.Campo),
            items.Count(i => i.Location == ItemLocation.Manutencao),
            techs.Count,
            d.CreatedAt);
}

using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class DepartmentService
{
    private readonly IDepartmentRepository _departments;
    private readonly IItemRepository       _items;

    public DepartmentService(IDepartmentRepository departments, IItemRepository items)
    {
        _departments = departments;
        _items       = items;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        var deps   = await _departments.GetAllAsync();
        var result = new List<DepartmentResponseDto>();
        foreach (var d in deps)
        {
            var depItems = await _items.GetByDepartmentAsync(d.Id);
            result.Add(ToDto(d, depItems));
        }
        return result;
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");

        var depItems = await _items.GetByDepartmentAsync(id);
        return ToDto(dep, depItems);
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var dep = new Department(Guid.NewGuid(), dto.Name.Trim(), dto.Description?.Trim());
        await _departments.AddAsync(dep);
        return ToDto(dep, []);
    }

    public async Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");

        dep.Update(dto.Name.Trim(), dto.Description?.Trim());
        await _departments.UpdateAsync(dep);

        var depItems = await _items.GetByDepartmentAsync(id);
        return ToDto(dep, depItems);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new BusinessException("Departamento não encontrado.", "department_not_found");
        await _departments.DeleteAsync(dep);
    }

    // ── Helper ─────────────────────────────────────────────────
    private static DepartmentResponseDto ToDto(Department d, List<Item> items) =>
        new(d.Id, d.Name, d.Description,
            items.Count,
            items.Count(i => i.Location == ItemLocation.Estoque),
            items.Count(i => i.Location == ItemLocation.Campo),
            items.Count(i => i.Location == ItemLocation.Manutencao),
            d.CreatedAt);
}

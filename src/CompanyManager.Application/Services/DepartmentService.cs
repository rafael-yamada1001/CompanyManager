using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class DepartmentService
{
    private readonly IDepartmentRepository _departments;
    private readonly IPersonRepository     _people;
    private readonly IItemRepository       _items;

    public DepartmentService(IDepartmentRepository departments, IPersonRepository people, IItemRepository items)
    {
        _departments = departments;
        _people      = people;
        _items       = items;
    }

    public async Task<List<DepartmentResponseDto>> GetAllAsync()
    {
        var deps = await _departments.GetAllAsync();
        var result = new List<DepartmentResponseDto>();

        foreach (var d in deps)
        {
            var depItems = await _items.GetByDepartmentAsync(d.Id);
            var persons  = await _people.GetByDepartmentAsync(d.Id);
            result.Add(ToDto(d, depItems, persons));
        }
        return result;
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new DomainException($"Departamento não encontrado.", "department_not_found");

        var depItems = await _items.GetByDepartmentAsync(id);
        var persons  = await _people.GetByDepartmentAsync(id);
        return ToDto(dep, depItems, persons);
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
            ?? throw new DomainException("Departamento não encontrado.", "department_not_found");

        dep.Update(dto.Name.Trim(), dto.Description?.Trim());
        await _departments.UpdateAsync(dep);

        var depItems = await _items.GetByDepartmentAsync(id);
        var persons  = await _people.GetByDepartmentAsync(id);
        return ToDto(dep, depItems, persons);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dep = await _departments.GetByIdAsync(id)
            ?? throw new DomainException("Departamento não encontrado.", "department_not_found");
        await _departments.DeleteAsync(dep);
    }

    // ── Pessoas ────────────────────────────────────────────────
    public async Task<List<PersonResponseDto>> GetPeopleAsync(Guid departmentId)
    {
        var depItems = await _items.GetByDepartmentAsync(departmentId);
        var people   = await _people.GetByDepartmentAsync(departmentId);

        return people.Select(p => new PersonResponseDto(
            p.Id, p.DepartmentId, p.Name,
            depItems.Count(i => i.PersonId == p.Id),
            p.CreatedAt
        )).ToList();
    }

    public async Task<PersonResponseDto> AddPersonAsync(Guid departmentId, CreatePersonDto dto)
    {
        _ = await _departments.GetByIdAsync(departmentId)
            ?? throw new DomainException("Departamento não encontrado.", "department_not_found");

        var person = new DepartmentPerson(Guid.NewGuid(), departmentId, dto.Name.Trim());
        await _people.AddAsync(person);
        return new PersonResponseDto(person.Id, person.DepartmentId, person.Name, 0, person.CreatedAt);
    }

    public async Task DeletePersonAsync(Guid departmentId, Guid personId)
    {
        var person = await _people.GetByIdAsync(personId)
            ?? throw new DomainException("Pessoa não encontrada.", "person_not_found");

        if (person.DepartmentId != departmentId)
            throw new DomainException("Pessoa não pertence a este departamento.", "person_not_found");

        await _people.DeleteAsync(person);
    }

    // ── Helper ─────────────────────────────────────────────────
    private static DepartmentResponseDto ToDto(Department d, List<Item> items, List<DepartmentPerson> people) =>
        new(d.Id, d.Name, d.Description,
            items.Count,
            items.Count(i => i.Location == ItemLocation.Estoque),
            items.Count(i => i.Location == ItemLocation.Campo),
            items.Count(i => i.Location == ItemLocation.Manutencao),
            people.Count,
            d.CreatedAt);
}

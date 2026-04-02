using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class ItemService
{
    private readonly IItemRepository   _items;
    private readonly IPersonRepository _people;

    public ItemService(IItemRepository items, IPersonRepository people)
    {
        _items  = items;
        _people = people;
    }

    public async Task<List<ItemResponseDto>> GetByDepartmentAsync(Guid departmentId)
    {
        var items   = await _items.GetByDepartmentAsync(departmentId);
        var people  = await _people.GetByDepartmentAsync(departmentId);
        return items.Select(i => ToDto(i, people)).ToList();
    }

    public async Task<ItemResponseDto> CreateAsync(Guid departmentId, CreateItemDto dto)
    {
        var item = new Item(Guid.NewGuid(), departmentId, dto.Name.Trim(), dto.Serial?.Trim(), dto.Category, dto.Observations?.Trim());
        await _items.AddAsync(item);
        return ToDto(item, []);
    }

    public async Task<ItemResponseDto> UpdateAsync(Guid departmentId, Guid itemId, UpdateItemDto dto)
    {
        var item = await GetItemOfDept(departmentId, itemId);
        item.Update(dto.Name.Trim(), dto.Serial?.Trim(), dto.Category, dto.Observations?.Trim());
        await _items.UpdateAsync(item);
        var people = await _people.GetByDepartmentAsync(departmentId);
        return ToDto(item, people);
    }

    public async Task<ItemResponseDto> MoveAsync(Guid departmentId, Guid itemId, MoveItemDto dto)
    {
        var item = await GetItemOfDept(departmentId, itemId);

        var location = dto.Location.ToLowerInvariant() switch
        {
            "estoque"    => ItemLocation.Estoque,
            "campo"      => ItemLocation.Campo,
            "manutencao" => ItemLocation.Manutencao,
            _            => throw new DomainException("Localização inválida.", "invalid_location")
        };

        if (location == ItemLocation.Campo && dto.PersonId is null)
            throw new DomainException("Informe a pessoa responsável ao mover para campo.", "person_required");

        item.Move(location, dto.PersonId, dto.Observations);
        await _items.UpdateAsync(item);
        var people = await _people.GetByDepartmentAsync(departmentId);
        return ToDto(item, people);
    }

    public async Task DeleteAsync(Guid departmentId, Guid itemId)
    {
        var item = await GetItemOfDept(departmentId, itemId);
        await _items.DeleteAsync(item);
    }

    // ── Helpers ────────────────────────────────────────────────
    private async Task<Item> GetItemOfDept(Guid departmentId, Guid itemId)
    {
        var item = await _items.GetByIdAsync(itemId)
            ?? throw new DomainException("Item não encontrado.", "item_not_found");

        if (item.DepartmentId != departmentId)
            throw new DomainException("Item não pertence a este departamento.", "item_not_found");

        return item;
    }

    private static ItemResponseDto ToDto(Item i, List<DepartmentPerson> people)
    {
        var personName = i.PersonId.HasValue
            ? people.FirstOrDefault(p => p.Id == i.PersonId)?.Name
            : null;

        return new ItemResponseDto(
            i.Id, i.DepartmentId, i.Name, i.Serial, i.Category,
            i.Location.ToString().ToLowerInvariant(),
            i.PersonId, personName, i.Observations, i.CreatedAt);
    }
}

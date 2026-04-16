using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Entities;
using CompanyManager.Domain.Enums;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class ItemService
{
    private readonly IItemRepository        _items;
    private readonly ITechnicianRepository  _technicians;

    public ItemService(IItemRepository items, ITechnicianRepository technicians)
    {
        _items       = items;
        _technicians = technicians;
    }

    public async Task<List<ItemResponseDto>> GetByDepartmentAsync(Guid departmentId)
    {
        var items = await _items.GetByDepartmentAsync(departmentId);
        var techs = await _technicians.GetAllAsync();
        return items.Select(i => ToDto(i, techs)).ToList();
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
        var techs = await _technicians.GetAllAsync();
        return ToDto(item, techs);
    }

    public async Task<ItemResponseDto> MoveAsync(Guid departmentId, Guid itemId, MoveItemDto dto)
    {
        var item = await GetItemOfDept(departmentId, itemId);

        var location = dto.Location.ToLowerInvariant() switch
        {
            "estoque"    => ItemLocation.Estoque,
            "campo"      => ItemLocation.Campo,
            "manutencao" => ItemLocation.Manutencao,
            _            => throw new BusinessException("Localização inválida.", "invalid_location")
        };

        if (location == ItemLocation.Campo && dto.PersonId is null)
            throw new BusinessException("Informe o técnico responsável ao mover para campo.", "technician_required");

        item.Move(location, dto.PersonId, dto.Observations);
        await _items.UpdateAsync(item);
        var techs = await _technicians.GetAllAsync();
        return ToDto(item, techs);
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
            ?? throw new BusinessException("Item não encontrado.", "item_not_found");

        if (item.DepartmentId != departmentId)
            throw new BusinessException("Item não pertence a este departamento.", "item_not_found");

        return item;
    }

    private static ItemResponseDto ToDto(Item i, List<Technician> techs)
    {
        var techName = i.PersonId.HasValue
            ? techs.FirstOrDefault(t => t.Id == i.PersonId)?.Name
            : null;

        return new ItemResponseDto(
            i.Id, i.DepartmentId, i.Name, i.Serial, i.Category,
            i.Location.ToString().ToLowerInvariant(),
            i.PersonId, techName, i.Observations, i.CreatedAt);
    }
}

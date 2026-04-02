using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Application.Services;
using CompanyManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("departments/{departmentId:guid}/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly ItemService           _service;
    private readonly IPermissionRepository _permissions;

    public ItemsController(ItemService service, IPermissionRepository permissions)
    {
        _service     = service;
        _permissions = permissions;
    }

    // GET /departments/{departmentId}/items
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid departmentId)
    {
        await RequireAccess(departmentId, PermissionLevel.Visualizar);
        return Ok(await _service.GetByDepartmentAsync(departmentId));
    }

    // POST /departments/{departmentId}/items
    [HttpPost]
    public async Task<IActionResult> Create(Guid departmentId, [FromBody] CreateItemDto dto)
    {
        await RequireAccess(departmentId, PermissionLevel.Editar);
        return Ok(await _service.CreateAsync(departmentId, dto));
    }

    // PUT /departments/{departmentId}/items/{itemId}
    [HttpPut("{itemId:guid}")]
    public async Task<IActionResult> Update(Guid departmentId, Guid itemId, [FromBody] UpdateItemDto dto)
    {
        await RequireAccess(departmentId, PermissionLevel.Editar);
        return Ok(await _service.UpdateAsync(departmentId, itemId, dto));
    }

    // POST /departments/{departmentId}/items/{itemId}/move
    [HttpPost("{itemId:guid}/move")]
    public async Task<IActionResult> Move(Guid departmentId, Guid itemId, [FromBody] MoveItemDto dto)
    {
        await RequireAccess(departmentId, PermissionLevel.Editar);
        return Ok(await _service.MoveAsync(departmentId, itemId, dto));
    }

    // DELETE /departments/{departmentId}/items/{itemId}
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid departmentId, Guid itemId)
    {
        await RequireAccess(departmentId, PermissionLevel.Gerenciar);
        await _service.DeleteAsync(departmentId, itemId);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────
    private bool IsAdmin() => User.IsInRole("admin");
    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task RequireAccess(Guid departmentId, PermissionLevel minLevel)
    {
        if (IsAdmin()) return;
        var ok = await _permissions.HasAccessAsync(CurrentUserId(), departmentId, minLevel);
        if (!ok) throw new UnauthorizedAccessException();
    }
}

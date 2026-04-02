using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Application.Services;
using CompanyManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly DepartmentService    _service;
    private readonly IPermissionRepository _permissions;

    public DepartmentsController(DepartmentService service, IPermissionRepository permissions)
    {
        _service     = service;
        _permissions = permissions;
    }

    // GET /departments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var all = await _service.GetAllAsync();

        if (IsAdmin()) return Ok(all);

        var userId = CurrentUserId();
        var perms  = await _permissions.GetByUserAsync(userId);
        var allowed = perms.Select(p => p.DepartmentId).ToHashSet();
        return Ok(all.Where(d => allowed.Contains(d.Id)));
    }

    // GET /departments/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await RequireAccess(id, PermissionLevel.Visualizar);
        return Ok(await _service.GetByIdAsync(id));
    }

    // POST /departments
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // PUT /departments/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentDto dto)
    {
        await RequireAccess(id, PermissionLevel.Gerenciar);
        return Ok(await _service.UpdateAsync(id, dto));
    }

    // DELETE /departments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // ── Técnicos ───────────────────────────────────────────────

    // GET /departments/{id}/technicians
    [HttpGet("{id:guid}/technicians")]
    public async Task<IActionResult> GetTechnicians(Guid id)
    {
        await RequireAccess(id, PermissionLevel.Visualizar);
        return Ok(await _service.GetTechniciansAsync(id));
    }

    // POST /departments/{id}/technicians
    [HttpPost("{id:guid}/technicians")]
    public async Task<IActionResult> AddTechnician(Guid id, [FromBody] CreateTechnicianDto dto)
    {
        await RequireAccess(id, PermissionLevel.Editar);
        return Ok(await _service.AddTechnicianAsync(id, dto));
    }

    // DELETE /departments/{id}/technicians/{technicianId}
    [HttpDelete("{id:guid}/technicians/{technicianId:guid}")]
    public async Task<IActionResult> DeleteTechnician(Guid id, Guid technicianId)
    {
        await RequireAccess(id, PermissionLevel.Gerenciar);
        await _service.DeleteTechnicianAsync(id, technicianId);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────
    private bool IsAdmin() =>
        User.IsInRole("admin");

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task RequireAccess(Guid departmentId, PermissionLevel minLevel)
    {
        if (IsAdmin()) return;
        var ok = await _permissions.HasAccessAsync(CurrentUserId(), departmentId, minLevel);
        if (!ok) throw new UnauthorizedAccessException();
    }
}

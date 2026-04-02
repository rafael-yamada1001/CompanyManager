using CompanyManager.Application.DTOs;
using CompanyManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("technicians")]
[Authorize]
public class TechniciansController : ControllerBase
{
    private readonly TechnicianService _service;
    public TechniciansController(TechnicianService service) => _service = service;

    // GET /technicians — lista todos os técnicos (requer acesso a técnicos)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        RequireTechnicianAccess();
        return Ok(await _service.GetAllAsync());
    }

    // GET /technicians/{id}/schedule — agenda do técnico
    [HttpGet("{id:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid id)
    {
        RequireTechnicianAccess();
        return Ok(await _service.GetScheduleAsync(id));
    }

    // POST /technicians/{id}/schedule — adiciona entrada na agenda
    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> AddSchedule(Guid id, [FromBody] CreateTechnicianScheduleDto dto)
    {
        RequireTechnicianAccess();
        return Ok(await _service.AddScheduleAsync(id, dto));
    }

    // PUT /technicians/schedule/{scheduleId} — atualiza entrada
    [HttpPut("schedule/{scheduleId:guid}")]
    public async Task<IActionResult> UpdateSchedule(Guid scheduleId, [FromBody] UpdateTechnicianScheduleDto dto)
    {
        RequireTechnicianAccess();
        return Ok(await _service.UpdateScheduleAsync(scheduleId, dto));
    }

    // DELETE /technicians/schedule/{scheduleId} — remove entrada
    [HttpDelete("schedule/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
    {
        RequireTechnicianAccess();
        await _service.DeleteScheduleAsync(scheduleId);
        return NoContent();
    }

    // ── Helper ─────────────────────────────────────────────────
    private void RequireTechnicianAccess()
    {
        var hasClaim = User.FindFirstValue("tech_access");
        if (hasClaim != "true")
            throw new UnauthorizedAccessException("Acesso à gestão de técnicos não autorizado.");
    }
}

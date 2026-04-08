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

    // GET /technicians — lista todos os técnicos (qualquer usuário autenticado pode consultar para atribuição de itens)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET /technicians/a-definir-id — retorna o ID fixo do técnico "A Definir"
    [HttpGet("a-definir-id")]
    public IActionResult GetADefinirId()
    {
        return Ok(new { id = TechnicianService.ADefinirId });
    }

    // POST /technicians
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTechnicianDto dto)
    {
        var tech = await _service.CreateAsync(dto);
        return Ok(tech);
    }

    // PUT /technicians/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTechnicianDto dto)
    {
        RequireTechnicianAccess();
        return Ok(await _service.UpdateAsync(id, dto));
    }

    // DELETE /technicians/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        RequireTechnicianAccess();
        await _service.DeleteAsync(id);
        return NoContent();
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

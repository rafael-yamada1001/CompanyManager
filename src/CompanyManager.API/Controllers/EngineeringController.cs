using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("engineering")]
[Authorize]
public class EngineeringController : ControllerBase
{
    private readonly IEngineeringService _service;
    public EngineeringController(IEngineeringService service) => _service = service;

    // GET /engineering/projects?query=&status=&client=
    [HttpGet("projects")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? query,
        [FromQuery] string? status,
        [FromQuery] string? client)
    {
        return Ok(await _service.GetAllAsync(query, status, client));
    }

    // POST /engineering/projects
    [HttpPost("projects")]
    public async Task<IActionResult> Create([FromBody] CreateEngineeringProjectDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    // GET /engineering/projects/{id}
    [HttpGet("projects/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    // PUT /engineering/projects/{id}
    [HttpPut("projects/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEngineeringProjectDto dto)
    {
        return Ok(await _service.UpdateAsync(id, dto));
    }

    // DELETE /engineering/projects/{id}
    [HttpDelete("projects/{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // GET /engineering/projects/{id}/documents
    [HttpGet("projects/{id:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        return Ok(await _service.GetDocumentsAsync(id));
    }

    // POST /engineering/projects/{id}/documents
    [HttpPost("projects/{id:guid}/documents")]
    public async Task<IActionResult> AddDocument(Guid id, [FromBody] CreateProjectDocumentDto dto)
    {
        return Ok(await _service.AddDocumentAsync(id, dto));
    }

    // PUT /engineering/documents/{docId}
    [HttpPut("documents/{docId:guid}")]
    public async Task<IActionResult> UpdateDocument(Guid docId, [FromBody] UpdateProjectDocumentDto dto)
    {
        return Ok(await _service.UpdateDocumentAsync(docId, dto));
    }

    // DELETE /engineering/documents/{docId}
    [HttpDelete("documents/{docId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid docId)
    {
        await _service.DeleteDocumentAsync(docId);
        return NoContent();
    }
}

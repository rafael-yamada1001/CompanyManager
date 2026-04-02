using CompanyManager.Application.DTOs;
using CompanyManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _service;
    public UsersController(UserService service) => _service = service;

    // GET /users  (admin only)
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    // GET /users/me/permissions
    [HttpGet("me/permissions")]
    public async Task<IActionResult> MyPermissions()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _service.GetMyPermissionsAsync(userId));
    }

    // GET /users/{id}  (admin only)
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetById(Guid id) =>
        Ok(await _service.GetByIdAsync(id));

    // POST /users  (admin only)
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // PUT /users/{id}  (admin only)
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto) =>
        Ok(await _service.UpdateAsync(id, dto));

    // PATCH /users/{id}/unblock  (admin only)
    [HttpPatch("{id:guid}/unblock")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        await _service.UnblockAsync(id);
        return NoContent();
    }

    // DELETE /users/{id}  (admin only)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // POST /users/{id}/permissions  (admin only)
    [HttpPost("{id:guid}/permissions")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetPermission(Guid id, [FromBody] SetPermissionDto dto)
    {
        await _service.SetPermissionAsync(id, dto);
        return NoContent();
    }

    // DELETE /users/{id}/permissions/{departmentId}  (admin only)
    [HttpDelete("{id:guid}/permissions/{departmentId:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RemovePermission(Guid id, Guid departmentId)
    {
        await _service.RemovePermissionAsync(id, departmentId);
        return NoContent();
    }

    // PATCH /users/{id}/technician-access  (admin only)
    [HttpPatch("{id:guid}/technician-access")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SetTechnicianAccess(Guid id, [FromBody] SetTechnicianAccessDto dto)
    {
        await _service.SetTechnicianAccessAsync(id, dto.HasAccess);
        return NoContent();
    }
}

public record SetTechnicianAccessDto(bool HasAccess);

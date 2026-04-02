using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Input;
using Microsoft.AspNetCore.Mvc;

namespace CompanyManager.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Realiza login e retorna um token JWT.</summary>
    /// <response code="200">Login realizado com sucesso.</response>
    /// <response code="401">Credenciais inválidas, usuário não encontrado ou bloqueado.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }
}

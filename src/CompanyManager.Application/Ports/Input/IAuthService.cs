using CompanyManager.Application.DTOs;

namespace CompanyManager.Application.Ports.Input;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}

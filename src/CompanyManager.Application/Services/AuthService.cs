using CompanyManager.Application.DTOs;
using CompanyManager.Application.Ports.Input;
using CompanyManager.Application.Ports.Output;
using CompanyManager.Domain.Exceptions;

namespace CompanyManager.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email);

        if (user is null)
            throw new UserNotFoundException();

        if (user.IsBlocked)
            throw new UserBlockedException();

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user);
            throw new InvalidCredentialsException();
        }

        user.ResetFailedLogins();
        await _userRepository.UpdateAsync(user);

        var token = _tokenService.GenerateToken(user);
        return new LoginResponseDto(token, _tokenService.ExpiresIn);
    }
}

namespace CompanyManager.Application.DTOs;

public record LoginResponseDto(string Token, int ExpiresIn);

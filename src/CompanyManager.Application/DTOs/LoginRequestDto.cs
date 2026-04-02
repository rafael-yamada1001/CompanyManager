using System.ComponentModel.DataAnnotations;

namespace CompanyManager.Application.DTOs;

public record LoginRequestDto(
    [Required] string Email,
    [Required] string Password
);

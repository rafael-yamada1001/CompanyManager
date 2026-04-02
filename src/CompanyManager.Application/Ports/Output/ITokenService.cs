using CompanyManager.Domain.Entities;

namespace CompanyManager.Application.Ports.Output;

public interface ITokenService
{
    string GenerateToken(User user);
    int ExpiresIn { get; }
}

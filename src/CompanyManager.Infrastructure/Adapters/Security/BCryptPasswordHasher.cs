using CompanyManager.Application.Ports.Output;

namespace CompanyManager.Infrastructure.Adapters.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);
}

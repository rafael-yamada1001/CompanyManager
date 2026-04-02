namespace CompanyManager.Application.Ports.Output;

public interface IPasswordHasher
{
    bool Verify(string password, string hash);
    string Hash(string password);
}

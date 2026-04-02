namespace CompanyManager.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Credenciais inválidas.", "invalid_credentials") { }
}

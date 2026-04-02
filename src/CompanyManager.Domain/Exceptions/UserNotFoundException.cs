namespace CompanyManager.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException()
        : base("Usuário ou e-mail não encontrado.", "user_not_found") { }
}

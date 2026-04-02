namespace CompanyManager.Domain.Exceptions;

public class UserBlockedException : DomainException
{
    public UserBlockedException()
        : base("Usuário bloqueado. Entre em contato com o administrador.", "user_blocked") { }
}

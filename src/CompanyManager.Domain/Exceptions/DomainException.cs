namespace CompanyManager.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string message, string code) : base(message)
    {
        Code = code;
    }
}

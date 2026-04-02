namespace CompanyManager.Domain.Exceptions;

public class BusinessException : DomainException
{
    public BusinessException(string message, string code) : base(message, code) { }
}

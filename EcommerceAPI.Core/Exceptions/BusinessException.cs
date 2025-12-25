namespace EcommerceAPI.Core.Exceptions;

public class BusinessException : DomainException
{
    public BusinessException(string message, string? errorCode = null) 
        : base(message, errorCode ?? "BUSINESS_ERROR")
    {
    }

    public BusinessException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException, errorCode ?? "BUSINESS_ERROR")
    {
    }
}

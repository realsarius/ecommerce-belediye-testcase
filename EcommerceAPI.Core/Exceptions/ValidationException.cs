namespace EcommerceAPI.Core.Exceptions;

public class ValidationException : DomainException
{
    public IDictionary<string, string[]>? Errors { get; }

    public ValidationException(string message)
        : base(message, "VALIDATION_ERROR")
    {
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Bir veya daha fazla doğrulama hatası oluştu", "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}

namespace EcommerceAPI.Core.Utilities.Results;

public class ErrorResult : Result
{
    public ErrorResult(string message, string? errorCode = null, object? details = null) 
        : base(false, message, errorCode, details)
    {
    }

    public ErrorResult() : base(false)
    {
    }
}

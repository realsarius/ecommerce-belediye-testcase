namespace EcommerceAPI.Core.Utilities.Results;

public class ErrorDataResult<T> : DataResult<T>
{
    public ErrorDataResult(T data, string message, string? errorCode = null, object? details = null) 
        : base(data, false, message, errorCode, details)
    {
    }

    public ErrorDataResult(T data) : base(data, false)
    {
    }

    public ErrorDataResult(string message, string? errorCode = null, object? details = null) 
        : base(default!, false, message, errorCode, details)
    {
    }

    public ErrorDataResult() : base(default!, false)
    {
    }
}

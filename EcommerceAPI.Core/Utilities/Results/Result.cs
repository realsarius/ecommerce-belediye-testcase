namespace EcommerceAPI.Core.Utilities.Results;

public class Result : IResult
{
    public Result(bool success, string message, string? errorCode = null, object? details = null)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
        Details = details;
    }

    public Result(bool success) : this(success, string.Empty)
    {
    }

    public bool Success { get; }
    public string Message { get; }
    public string? ErrorCode { get; }
    public object? Details { get; }
}

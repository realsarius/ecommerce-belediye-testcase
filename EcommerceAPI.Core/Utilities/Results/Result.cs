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

    public bool Success { get; private set; }
    public string Message { get; private set; }
    public string? ErrorCode { get; private set; }
    public object? Details { get; private set; }
}

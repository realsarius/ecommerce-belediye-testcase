namespace EcommerceAPI.Core.Utilities.Results;

public interface IResult
{
    bool Success { get; }
    string Message { get; }
    string? ErrorCode { get; }
    object? Details { get; }
}

namespace EcommerceAPI.Core.Utilities.Results;

/// <summary>
/// Temel sonuç sınıfı.
/// </summary>
public class Result : IResult
{
    public Result(bool success, string message) : this(success)
    {
        Message = message;
    }

    public Result(bool success)
    {
        Success = success;
        Message = string.Empty;
    }

    public bool Success { get; }
    public string Message { get; }
}

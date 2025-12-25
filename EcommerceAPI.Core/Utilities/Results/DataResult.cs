namespace EcommerceAPI.Core.Utilities.Results;

public class DataResult<T> : Result, IDataResult<T>
{
    public DataResult(T data, bool success, string message, string? errorCode = null, object? details = null) 
        : base(success, message, errorCode, details)
    {
        Data = data;
    }

    public DataResult(T data, bool success) : base(success)
    {
        Data = data;
    }

    public T Data { get; }
}

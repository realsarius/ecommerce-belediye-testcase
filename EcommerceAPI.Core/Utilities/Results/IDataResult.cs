namespace EcommerceAPI.Core.Utilities.Results;

/// <summary>
/// Data içeren sonuç interface.
/// </summary>
public interface IDataResult<out T> : IResult
{
    T Data { get; }
}

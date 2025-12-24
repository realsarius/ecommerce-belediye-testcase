namespace EcommerceAPI.Core.Utilities.Results;

/// <summary>
/// Temel sonuç interface.
/// Başarı durumu ve mesaj içerir.
/// </summary>
public interface IResult
{
    bool Success { get; }
    string Message { get; }
}

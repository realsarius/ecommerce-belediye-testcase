namespace EcommerceAPI.Core.Utilities.Results;

/// <summary>
/// Başarılı sonuç.
/// </summary>
public class SuccessResult : Result
{
    public SuccessResult(string message) : base(true, message)
    {
    }

    public SuccessResult() : base(true)
    {
    }
}

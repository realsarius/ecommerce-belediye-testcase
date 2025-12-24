namespace EcommerceAPI.Core.Utilities.Results;

/// <summary>
/// Hatalı sonuç.
/// </summary>
public class ErrorResult : Result
{
    public ErrorResult(string message) : base(false, message)
    {
    }

    public ErrorResult() : base(false)
    {
    }
}

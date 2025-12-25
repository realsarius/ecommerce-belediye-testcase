namespace EcommerceAPI.Core.Utilities.Results;

public class SuccessResult : Result
{
    public SuccessResult(string message) : base(true, message, null, null)
    {
    }

    public SuccessResult() : base(true)
    {
    }
}

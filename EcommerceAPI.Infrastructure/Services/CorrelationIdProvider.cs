using EcommerceAPI.Core.CrossCuttingConcerns;

namespace EcommerceAPI.Infrastructure.Services;

public class CorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public string? GetCorrelationId()
    {
        return _correlationId.Value;
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = correlationId;
    }
}

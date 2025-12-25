namespace EcommerceAPI.Core.CrossCuttingConcerns;

public interface ICorrelationIdProvider
{
    string? GetCorrelationId();
    void SetCorrelationId(string correlationId);
}

namespace EcommerceAPI.Core.Interfaces;

public interface IIyzicoRefundGateway
{
    Task<IyzicoRefundGatewayResult> RefundAsync(IyzicoRefundGatewayRequest request, CancellationToken cancellationToken = default);
}

public sealed record IyzicoRefundGatewayRequest(
    string PaymentId,
    decimal Amount,
    string Currency,
    string Ip,
    string? Reason = null,
    string? Description = null);

public sealed record IyzicoRefundGatewayResult(
    bool Success,
    string? ProviderReferenceId,
    string? ErrorMessage,
    string? ErrorCode);

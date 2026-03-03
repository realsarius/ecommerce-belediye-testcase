using Iyzipay.Request;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public interface IIyzicoPaymentGateway
{
    Task<IyzicoChargeGatewayResult> ChargeAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IyzicoThreeDSInitializeGatewayResult> InitializeThreeDSAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record IyzicoChargeGatewayResult(
    bool Success,
    string? PaymentId,
    string? ErrorMessage,
    string? CardToken,
    string? CardUserKey,
    string? LastFourDigits);

public sealed record IyzicoThreeDSInitializeGatewayResult(
    bool Success,
    string? PaymentId,
    string? HtmlContent,
    string? ErrorMessage);

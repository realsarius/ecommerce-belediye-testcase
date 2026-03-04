using EcommerceAPI.Infrastructure.Settings;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class IyzicoPaymentGateway : IIyzicoPaymentGateway
{
    private readonly IyzicoSettings _settings;

    public IyzicoPaymentGateway(IOptions<IyzicoSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<IyzicoChargeGatewayResult> ChargeAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = CreateOptions();
        var response = await Iyzipay.Model.Payment.Create(request, options);

        return new IyzicoChargeGatewayResult(
            string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase),
            response.PaymentId,
            response.ErrorMessage,
            response.CardToken,
            response.CardUserKey,
            response.LastFourDigits);
    }

    public async Task<IyzicoThreeDSInitializeGatewayResult> InitializeThreeDSAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = CreateOptions();
        var response = await ThreedsInitialize.Create(request, options);

        return new IyzicoThreeDSInitializeGatewayResult(
            string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase),
            response.PaymentId,
            response.HtmlContent,
            response.ErrorMessage);
    }

    private Iyzipay.Options CreateOptions()
    {
        return new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };
    }
}

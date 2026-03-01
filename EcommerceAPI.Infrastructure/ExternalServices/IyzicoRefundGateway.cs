using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Infrastructure.Settings;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.ExternalServices;

public class IyzicoRefundGateway : IIyzicoRefundGateway
{
    private readonly IyzicoSettings _settings;

    public IyzicoRefundGateway(IOptions<IyzicoSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<IyzicoRefundGatewayResult> RefundAsync(
        IyzicoRefundGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new Iyzipay.Options
        {
            ApiKey = _settings.ApiKey,
            SecretKey = _settings.SecretKey,
            BaseUrl = _settings.BaseUrl
        };

        var refundRequest = new CreateAmountBasedRefundRequest
        {
            PaymentId = request.PaymentId,
            Price = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            Ip = request.Ip
        };

        var response = await Refund.CreateAmountBasedRefundRequest(refundRequest, options);

        return new IyzicoRefundGatewayResult(
            string.Equals(response.Status, "success", StringComparison.OrdinalIgnoreCase),
            response.HostReference ?? response.PaymentId,
            response.ErrorMessage,
            response.ErrorCode);
    }
}

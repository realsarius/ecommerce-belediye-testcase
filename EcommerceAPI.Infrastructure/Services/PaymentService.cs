using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IOrderDal _orderDal;
    private readonly PaymentSettings _paymentSettings;

    public PaymentService(
        IPaymentProviderFactory paymentProviderFactory,
        IOrderDal orderDal,
        IOptions<PaymentSettings> paymentSettings)
    {
        _paymentProviderFactory = paymentProviderFactory;
        _orderDal = orderDal;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<IDataResult<PaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
    {
        var providerType = request.PaymentProvider ?? _paymentSettings.DefaultProvider;
        request.PaymentProvider = providerType;

        if (!_paymentSettings.ActiveProviders.Contains(providerType))
        {
            return new ErrorDataResult<PaymentDto>($"Secilen odeme saglayicisi aktif degil: {providerType}");
        }

        try
        {
            return await _paymentProviderFactory.GetProvider(providerType).ProcessPaymentAsync(userId, request);
        }
        catch (NotSupportedException ex)
        {
            return new ErrorDataResult<PaymentDto>(ex.Message);
        }
    }

    public async Task<IDataResult<PaymentDto>> GetPaymentByOrderIdAsync(int orderId)
    {
        var order = await _orderDal.GetByIdWithDetailsAsync(orderId);
        if (order?.Payment == null)
        {
            return new ErrorDataResult<PaymentDto>("Odeme bulunamadi");
        }

        var providerType = order.Payment.Provider ?? PaymentProviderType.Iyzico;

        try
        {
            return await _paymentProviderFactory.GetProvider(providerType).GetPaymentByOrderIdAsync(orderId);
        }
        catch (NotSupportedException ex)
        {
            return new ErrorDataResult<PaymentDto>(ex.Message);
        }
    }

    public Task<IResult> ProcessWebhookAsync(IyzicoWebhookRequest request, string signatureHeader)
    {
        return _paymentProviderFactory.GetProvider(PaymentProviderType.Iyzico)
            .ProcessWebhookAsync(request, signatureHeader);
    }

    public Task<IResult> VerifyAndFinalizePaymentAsync(string paymentId, string conversationId, string conversationData)
    {
        return _paymentProviderFactory.GetProvider(PaymentProviderType.Iyzico)
            .VerifyAndFinalizePaymentAsync(paymentId, conversationId, conversationData);
    }
}

using EcommerceAPI.Business.Abstract;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderDal _orderDal;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IPaymentService paymentService,
        IOrderDal orderDal,
        ILogger<PaymentWebhookController> logger)
    {
        _paymentService = paymentService;
        _orderDal = orderDal;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(
        [FromForm] IyzicoWebhookRequest request,
        [FromHeader(Name = "X-IYZ-SIGNATURE-V3")] string? signature)
    {
        _logger.LogInformation(
            "Webhook received: EventType={EventType}, PaymentId={PaymentId}, ConversationId={ConversationId}, Status={Status}",
            request.IyziEventType, request.PaymentId, request.PaymentConversationId, request.Status);

        try
        {

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook rejected: Missing X-IYZ-SIGNATURE-V3 header");

            }

            var result = await _paymentService.ProcessWebhookAsync(request, signature ?? string.Empty);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Webhook processed successfully: ConversationId={ConversationId}",
                    request.PaymentConversationId);
                return Ok(new { message = "Webhook processed successfully" });
            }
            else
            {
                _logger.LogWarning(
                    "Webhook processing failed: ConversationId={ConversationId}",
                    request.PaymentConversationId);
                // iyzico retry durdurması için yine 200 dönmek gerekebilir
                return Ok(new { message = "Webhook received but not processed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook error: ConversationId={ConversationId}", request.PaymentConversationId);
            // iyzico'ya 200 dönerek retry'ı durdurmak bazen tercih edilir
            return Ok(new { message = "Webhook error logged" });
        }
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> HandleCallback(
        [FromForm] string? paymentId,
        [FromForm] string? conversationId,
        [FromForm] string? conversationData,
        [FromForm] string? status,
        [FromForm] string? mdStatus)
    {
        var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
        var redirectOrderId = await ResolveOrderIdAsync(conversationId);

        if (string.IsNullOrEmpty(paymentId) ||
            string.IsNullOrEmpty(conversationId) ||
            string.IsNullOrEmpty(conversationData))
        {
            _logger.LogWarning(
                "Callback rejected: Missing paymentId, conversationId or conversationData. ConversationId={ConversationId}",
                conversationId);
            return Redirect(BuildFrontendRedirectUrl(frontendBaseUrl, redirectOrderId, "failed"));
        }

        _logger.LogInformation(
            "3DS callback received: ConversationId={ConversationId}, PaymentId={PaymentId}, Status={Status}, MdStatus={MdStatus}",
            conversationId,
            paymentId,
            status,
            mdStatus);

        try
        {
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "3DS callback returned unsuccessful status. ConversationId={ConversationId}, Status={Status}, MdStatus={MdStatus}",
                    conversationId,
                    status,
                    mdStatus);
                return Redirect(BuildFrontendRedirectUrl(frontendBaseUrl, redirectOrderId, "failed"));
            }

            var result = await _paymentService.VerifyAndFinalizePaymentAsync(paymentId, conversationId, conversationData);
            
            if (result.Success)
            {
                _logger.LogInformation("Payment verified successfully: ConversationId={ConversationId}", conversationId);
                return Redirect(BuildFrontendRedirectUrl(frontendBaseUrl, redirectOrderId, "success"));
            }
            else
            {
                _logger.LogWarning("Payment verification failed: ConversationId={ConversationId}", conversationId);
                return Redirect(BuildFrontendRedirectUrl(frontendBaseUrl, redirectOrderId, "failed"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback error: ConversationId={ConversationId}", conversationId);
            return Redirect(BuildFrontendRedirectUrl(frontendBaseUrl, redirectOrderId, "failed"));
        }
    }

    private async Task<int?> ResolveOrderIdAsync(string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        var order = await _orderDal.GetByOrderNumberAsync(conversationId);
        return order?.Id;
    }

    private static string BuildFrontendRedirectUrl(string frontendBaseUrl, int? orderId, string status)
    {
        if (orderId.HasValue)
        {
            return $"{frontendBaseUrl}/orders/{orderId.Value}?payment={status}";
        }

        return $"{frontendBaseUrl}/checkout?threeDS={status}";
    }
}

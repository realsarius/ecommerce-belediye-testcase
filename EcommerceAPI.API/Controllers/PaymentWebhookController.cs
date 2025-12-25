using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentWebhookController> _logger;

    public PaymentWebhookController(
        IPaymentService paymentService,
        ILogger<PaymentWebhookController> logger)
    {
        _paymentService = paymentService;
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
        [FromForm] string? token,
        [FromForm] string? conversationId)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(conversationId))
        {
            _logger.LogWarning("Callback rejected: Missing token or conversationId");
            return BadRequest(new { message = "Token ve conversationId gereklidir" });
        }

        _logger.LogInformation("Callback received: ConversationId={ConversationId}", conversationId);

        try
        {
            var result = await _paymentService.VerifyAndFinalizePaymentAsync(token, conversationId);


            var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
            
            if (result.Success)
            {
                _logger.LogInformation("Payment verified successfully: ConversationId={ConversationId}", conversationId);
                return Redirect($"{frontendBaseUrl}/payment/success?orderId={conversationId}");
            }
            else
            {
                _logger.LogWarning("Payment verification failed: ConversationId={ConversationId}", conversationId);
                return Redirect($"{frontendBaseUrl}/payment/failed?orderId={conversationId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback error: ConversationId={ConversationId}", conversationId);
            var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
            return Redirect($"{frontendBaseUrl}/payment/error?orderId={conversationId}");
        }
    }
}


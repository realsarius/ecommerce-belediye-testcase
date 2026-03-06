using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Application.Abstractions.Persistence;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Infrastructure.Constants;
using EcommerceAPI.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentWebhookController : ControllerBase
{
    private static readonly Meter WebhookMeter = new("EcommerceAPI.PaymentWebhook");
    private static readonly Counter<long> WebhookEventsCounter = WebhookMeter.CreateCounter<long>(
        "payment_webhook_events_total",
        unit: "events",
        description: "Payment webhook requests grouped by processing outcome");

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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleWebhook(
        [FromForm] IyzicoWebhookRequest request,
        [FromHeader(Name = "X-IYZ-SIGNATURE-V3")] string? signature)
    {
        var sanitizedPaymentId = SensitiveDataLogSanitizer.Sanitize(request.PaymentId);
        var sanitizedConversationId = SensitiveDataLogSanitizer.Sanitize(request.PaymentConversationId);

        _logger.LogInformation(
            "Webhook received: EventType={EventType}, PaymentId={PaymentId}, ConversationId={ConversationId}, Status={Status}",
            request.IyziEventType, sanitizedPaymentId, sanitizedConversationId, request.Status);

        try
        {
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook rejected: Missing X-IYZ-SIGNATURE-V3 header");
            }

            var result = await _paymentService.ProcessWebhookAsync(request, signature ?? string.Empty);

            if (result.Success)
            {
                var outcome = string.Equals(result.Message, "Webhook already processed", StringComparison.OrdinalIgnoreCase)
                    ? "duplicate"
                    : "accepted";
                RecordWebhookMetric(outcome, StatusCodes.Status200OK);

                _logger.LogInformation(
                    "Webhook processed successfully: ConversationId={ConversationId}",
                    sanitizedConversationId);
                return Ok(new { message = string.IsNullOrWhiteSpace(result.Message) ? "Webhook processed successfully" : result.Message });
            }

            _logger.LogWarning(
                "Webhook processing failed: ConversationId={ConversationId}, ErrorCode={ErrorCode}",
                sanitizedConversationId,
                result.ErrorCode);

            return result.ErrorCode switch
            {
                InfrastructureConstants.Payment.WebhookInvalidSignatureCode
                    => BuildMetricResult("invalid_signature", StatusCodes.Status401Unauthorized, Unauthorized(new { message = "Webhook signature is invalid" })),
                InfrastructureConstants.Payment.WebhookConversationIdMissingCode
                    => BuildMetricResult("missing_conversation_id", StatusCodes.Status400BadRequest, BadRequest(new { message = "ConversationId is missing" })),
                InfrastructureConstants.Payment.OrderNotFoundCode or InfrastructureConstants.Payment.PaymentRecordNotFoundCode
                    => BuildMetricResult("not_found", StatusCodes.Status404NotFound, NotFound(new { message = result.Message })),
                _ => BuildMetricResult("rejected", StatusCodes.Status422UnprocessableEntity, UnprocessableEntity(new { message = result.Message, errorCode = result.ErrorCode }))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook error: ConversationId={ConversationId}", sanitizedConversationId);
            RecordWebhookMetric("error", StatusCodes.Status500InternalServerError);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Webhook processing failed" });
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

    private static IActionResult BuildMetricResult(string outcome, int statusCode, IActionResult result)
    {
        RecordWebhookMetric(outcome, statusCode);
        return result;
    }

    private static void RecordWebhookMetric(string outcome, int statusCode)
    {
        WebhookEventsCounter.Add(
            1,
            new KeyValuePair<string, object?>("provider", "iyzico"),
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }
}

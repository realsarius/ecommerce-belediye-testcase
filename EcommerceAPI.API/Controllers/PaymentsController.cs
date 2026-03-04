using System.Linq;
using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Enums;
using EcommerceAPI.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("payment")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICartService _cartService;
    private readonly PaymentSettings _paymentSettings;

    public PaymentsController(
        IPaymentService paymentService,
        ICartService cartService,
        IOptions<PaymentSettings> paymentSettings)
    {
        _paymentService = paymentService;
        _cartService = cartService;
        _paymentSettings = paymentSettings.Value;
    }

    private int GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği");
        return userId;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        ApplyIdempotencyKeyHeader(request);

        var userId = GetUserId();
        var result = await _paymentService.ProcessPaymentAsync(userId, request);
        
        if (result.Success)
        {
            if (result.Data is null || !result.Data.RequiresThreeDS)
            {
                await _cartService.ClearCartAsync(userId);
            }
            return Created("", result);
        }
        return BadRequest(result);
    }

    [HttpGet("settings")]
    public IActionResult GetPaymentSettings()
    {
        var configuredProviders = _paymentSettings.ActiveProviders?.Count > 0
            ? _paymentSettings.ActiveProviders
            : [PaymentProviderType.Iyzico];

        var activeProviders = configuredProviders
            .Distinct()
            .ToList();

        var defaultProvider = activeProviders.Contains(_paymentSettings.DefaultProvider)
            ? _paymentSettings.DefaultProvider
            : activeProviders[0];

        return Ok(new SuccessDataResult<PaymentSettingsDto>(new PaymentSettingsDto
        {
            ActiveProviders = activeProviders,
            DefaultProvider = defaultProvider,
            EnableMultiProviderSelection = _paymentSettings.EnableMultiProviderSelection,
            EnableTokenizedCardSave = _paymentSettings.EnableTokenizedCardSave,
            AllowLegacyEncryptedSavedCardPayments = _paymentSettings.AllowLegacyEncryptedSavedCardPayments,
            Force3DSecure = _paymentSettings.Force3DSecure,
            Force3DSecureAbove = _paymentSettings.Force3DSecureAbove
        }));
    }

    private void ApplyIdempotencyKeyHeader(ProcessPaymentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return;
        }

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var headerValues))
        {
            return;
        }

        var headerKey = headerValues.ToString().Trim();
        if (!string.IsNullOrEmpty(headerKey))
        {
            request.IdempotencyKey = headerKey;
        }
    }
}

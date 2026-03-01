using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[EnableRateLimiting("payment")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICartService _cartService;

    public PaymentsController(IPaymentService paymentService, ICartService cartService)
    {
        _paymentService = paymentService;
        _cartService = cartService;
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
            await _cartService.ClearCartAsync(userId);
            return Created("", result);
        }
        return BadRequest(result);
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

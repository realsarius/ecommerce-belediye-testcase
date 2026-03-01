using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceAPI.Core.Interfaces;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/contact")]
[AllowAnonymous]
public class ContactController : BaseApiController
{
    private static readonly CreateContactMessageRequestValidator Validator = new();
    private readonly IContactMessageService _contactMessageService;
    private readonly IContactRateLimitService _contactRateLimitService;

    public ContactController(
        IContactMessageService contactMessageService,
        IContactRateLimitService contactRateLimitService)
    {
        _contactMessageService = contactMessageService;
        _contactRateLimitService = contactRateLimitService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContactMessageRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await Validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errors = validationResult.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    message = x.ErrorMessage
                })
            });
        }

        var ipAddress = Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor)
            ? forwardedFor.ToString().Split(',')[0].Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var rateLimit = await _contactRateLimitService.TryConsumeAsync(ipAddress, cancellationToken);
        if (!rateLimit.Allowed)
        {
            Response.Headers.RetryAfter = rateLimit.RetryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                success = false,
                message = "Bu IP adresi için saatlik iletişim formu limiti aşıldı. Lütfen daha sonra tekrar deneyin.",
                retryAfterSeconds = rateLimit.RetryAfterSeconds
            });
        }

        var result = await _contactMessageService.CreateAsync(
            request,
            ipAddress,
            Request.Headers.UserAgent.ToString());

        return HandleResult(result);
    }
}

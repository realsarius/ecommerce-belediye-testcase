using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Business.Constants;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }

    [HttpPost("social")]
    public async Task<IActionResult> Social([FromBody] SocialLoginRequest request)
    {
        var result = await _authService.SocialLoginAsync(request);

        if (!result.Success)
            return Unauthorized(result);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost("verify-email-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmailCode([FromBody] VerifyEmailCodeRequest request)
    {
        var result = await _authService.VerifyEmailCodeAsync(request);
        if (result.Success)
        {
            return Ok(result);
        }

        if (result.ErrorCode == ErrorCodes.TooManyAttempts)
        {
            var retryAfterSeconds = ExtractRetryAfterSeconds(result.Details);
            if (retryAfterSeconds > 0)
            {
                Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            }

            return StatusCode(StatusCodes.Status429TooManyRequests, result);
        }

        return BadRequest(result);
    }

    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.ResendVerificationAsync(userId);
        if (!result.Success && result.ErrorCode == ErrorCodes.RateLimitExceeded)
        {
            var retryAfterSeconds = ExtractRetryAfterSeconds(result.Details);
            if (retryAfterSeconds > 0)
            {
                Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            }

            return StatusCode(StatusCodes.Status429TooManyRequests, result);
        }

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("resend-verification-code")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendVerificationCodeRequest request)
    {
        var result = await _authService.ResendVerificationCodeAsync(request);
        if (!result.Success && result.ErrorCode == ErrorCodes.RateLimitExceeded)
        {
            var retryAfterSeconds = ExtractRetryAfterSeconds(result.Details);
            if (retryAfterSeconds > 0)
            {
                Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            }

            return StatusCode(StatusCodes.Status429TooManyRequests, result);
        }

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        if (!result.Success && result.ErrorCode == ErrorCodes.RateLimitExceeded)
        {
            var retryAfterSeconds = ExtractRetryAfterSeconds(result.Details);
            if (retryAfterSeconds > 0)
            {
                Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            }

            return StatusCode(StatusCodes.Status429TooManyRequests, result);
        }

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("confirm-email-change")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        var result = await _authService.ConfirmEmailChangeAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _authService.GetUserByIdAsync(userId);
        if (!result.Success) return Unauthorized(result);

        return Ok(result);
    }

    private static int ExtractRetryAfterSeconds(object? details)
    {
        if (details is not { })
        {
            return 0;
        }

        var property = details.GetType().GetProperty("RetryAfterSeconds");
        if (property == null)
        {
            property = details.GetType().GetProperty("retryAfterSeconds");
        }

        if (property?.GetValue(details) is int retryAfterSeconds)
        {
            return retryAfterSeconds;
        }

        return 0;
    }
}

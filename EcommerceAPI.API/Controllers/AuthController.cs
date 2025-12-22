using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Success)
            return Unauthorized(result);
            
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
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
        
        if (!result)
            return BadRequest(new { message = "Token geçersiz" });

        return Ok(new { message = "Token iptal edildi" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return Unauthorized();

        return Ok(new AuthResponse { Success = true, User = user });
    }
}

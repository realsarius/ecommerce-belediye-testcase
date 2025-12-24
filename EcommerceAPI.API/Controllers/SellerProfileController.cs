using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/profile")]
[Authorize(Roles = "Seller")]
public class SellerProfileController : ControllerBase
{
    private readonly ISellerProfileService _sellerProfileService;

    public SellerProfileController(ISellerProfileService sellerProfileService)
    {
        _sellerProfileService = sellerProfileService;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        var result = await _sellerProfileService.GetByUserIdAsync(userId.Value);
        
        if (result.Success)
            return Ok(result);
        
        return NotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] CreateSellerProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        var result = await _sellerProfileService.CreateAsync(userId.Value, request);
        
        if (result.Success)
            return CreatedAtAction(nameof(GetMyProfile), result);
        
        return BadRequest(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        var result = await _sellerProfileService.UpdateAsync(userId.Value, request);
        
        if (result.Success)
            return Ok(result);
        
        return BadRequest(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        var result = await _sellerProfileService.DeleteAsync(userId.Value);
        
        if (result.Success)
            return Ok(result);
        
        return BadRequest(result);
    }

    [HttpGet("exists")]
    public async Task<IActionResult> HasProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        var hasProfile = await _sellerProfileService.HasProfileAsync(userId.Value);
        return Ok(new { hasProfile });
    }
}

// Admin endpoint to view any seller profile
[ApiController]
[Route("api/v1/admin/sellers")]
[Authorize(Roles = "Admin")]
public class AdminSellerProfileController : ControllerBase
{
    private readonly ISellerProfileService _sellerProfileService;

    public AdminSellerProfileController(ISellerProfileService sellerProfileService)
    {
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSellerProfile(int id)
    {
        var result = await _sellerProfileService.GetByIdAsync(id);
        
        if (result.Success)
            return Ok(result);
        
        return NotFound(result);
    }
}

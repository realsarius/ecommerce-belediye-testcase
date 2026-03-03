using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/profile")]
[Authorize(Roles = "Seller")]
public class SellerProfileController : SellerApiControllerBase
{
    private readonly ISellerProfileService _sellerProfileService;

    public SellerProfileController(ISellerProfileService sellerProfileService)
    {
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return InvalidSellerSession();

        var result = await _sellerProfileService.GetByUserIdAsync(userId.Value);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] CreateSellerProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return InvalidSellerSession();

        var result = await _sellerProfileService.CreateAsync(userId.Value, request);
        return HandleCreatedResult(result, nameof(GetMyProfile), new { });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return InvalidSellerSession();

        var result = await _sellerProfileService.UpdateAsync(userId.Value, request);
        return HandleResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return InvalidSellerSession();

        var result = await _sellerProfileService.DeleteAsync(userId.Value);
        return HandleDeleteResult(result);
    }

    [HttpGet("exists")]
    public async Task<IActionResult> HasProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return InvalidSellerSession();

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

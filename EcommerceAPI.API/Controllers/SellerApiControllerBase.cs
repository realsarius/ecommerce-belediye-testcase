using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

public abstract class SellerApiControllerBase : BaseApiController
{
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        return userId;
    }

    protected async Task<SellerRequestContext?> GetSellerContextAsync(ISellerProfileService sellerProfileService)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return null;
        }

        var profileResult = await sellerProfileService.GetByUserIdAsync(userId.Value);
        return new SellerRequestContext(userId.Value, profileResult.Success ? profileResult.Data?.Id : null);
    }

    protected IActionResult InvalidSellerSession()
    {
        return Unauthorized(new { success = false, message = "Geçersiz kullanıcı oturumu." });
    }

    protected IActionResult MissingSellerProfile(string message = "Satıcı profili bulunamadı.")
    {
        return BadRequest(new { success = false, message });
    }

    protected sealed record SellerRequestContext(int UserId, int? SellerProfileId);
}

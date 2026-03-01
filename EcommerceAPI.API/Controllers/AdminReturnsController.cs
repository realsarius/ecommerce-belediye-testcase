using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[Authorize(Roles = "Admin,Seller")]
[ApiController]
[Route("api/v1/admin/returns")]
public class AdminReturnsController : BaseApiController
{
    private readonly IReturnRequestService _returnRequestService;
    private readonly ISellerProfileService _sellerProfileService;

    public AdminReturnsController(
        IReturnRequestService returnRequestService,
        ISellerProfileService sellerProfileService)
    {
        _returnRequestService = returnRequestService;
        _sellerProfileService = sellerProfileService;
    }

    private (int? UserId, string? Role) GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        int? userId = null;
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        return (userId, roleClaim?.Value);
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingReturnRequests()
    {
        var (userId, role) = GetCurrentUser();
        int? sellerId = null;

        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
            {
                return BadRequest("Satıcı profili bulunamadı.");
            }

            sellerId = profileResult.Data.Id;
        }

        var result = await _returnRequestService.GetPendingReturnRequestsAsync(sellerId);
        return HandleResult(result);
    }

    [HttpPatch("{id:int:min(1)}")]
    public async Task<IActionResult> ReviewReturnRequest(int id, [FromBody] ReviewReturnRequestRequest request)
    {
        var (userId, role) = GetCurrentUser();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        int? sellerId = null;
        if (role == "Seller")
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
            {
                return BadRequest("Satıcı profili bulunamadı.");
            }

            sellerId = profileResult.Data.Id;
        }

        var result = await _returnRequestService.ReviewReturnRequestAsync(id, userId.Value, request, sellerId);
        return HandleResult(result);
    }
}

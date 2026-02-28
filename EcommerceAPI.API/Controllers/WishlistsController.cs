using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EcommerceAPI.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class WishlistsController : BaseApiController
{
    private readonly IWishlistService _wishlistService;

    public WishlistsController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }

        return 0;
    }

    [HttpGet]
    [EnableRateLimiting("wishlist-read")]
    public async Task<IActionResult> GetWishlist([FromQuery] string? cursor = null, [FromQuery] int? limit = null, [FromQuery] int? collectionId = null)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.GetWishlistByUserIdAsync(userId, cursor, limit, collectionId);
        return HandleResult(result);
    }

    [HttpGet("collections")]
    [EnableRateLimiting("wishlist-read")]
    public async Task<IActionResult> GetCollections()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.GetCollectionsAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("collections")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> CreateCollection([FromBody] CreateWishlistCollectionRequest request)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.CreateCollectionAsync(userId, request);
        return HandleResult(result);
    }

    [HttpGet("share")]
    [EnableRateLimiting("wishlist-read")]
    public async Task<IActionResult> GetShareSettings()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.GetShareSettingsAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("share")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> EnableSharing()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.EnableSharingAsync(userId);
        return HandleResult(result);
    }

    [HttpDelete("share")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> DisableSharing()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.DisableSharingAsync(userId);
        return HandleResult(result);
    }

    [AllowAnonymous]
    [HttpGet("share/{shareToken:guid}")]
    [EnableRateLimiting("wishlist-read")]
    public async Task<IActionResult> GetSharedWishlist(Guid shareToken, [FromQuery] string? cursor = null, [FromQuery] int? limit = null)
    {
        var result = await _wishlistService.GetPublicWishlistByShareTokenAsync(shareToken, cursor, limit);
        return HandleResult(result);
    }

    [HttpPost("items")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemRequest dto)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.AddItemToWishlistAsync(userId, dto.ProductId, dto.CollectionId);
        return HandleResult(result);
    }

    [HttpPatch("items/{productId:int:min(1)}/collection")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> MoveItemToCollection(int productId, [FromBody] MoveWishlistItemToCollectionRequest request)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.MoveItemToCollectionAsync(userId, productId, request.CollectionId);
        return HandleResult(result);
    }

    [HttpDelete("items/{productId:int:min(1)}")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.RemoveItemFromWishlistAsync(userId, productId);
        return HandleResult(result);
    }

    [HttpDelete]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> ClearWishlist()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.ClearWishlistAsync(userId);
        return HandleResult(result);
    }

    [HttpPost("add-all-to-cart")]
    [EnableRateLimiting("wishlist")]
    public async Task<IActionResult> AddAllToCart()
    {
        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var result = await _wishlistService.AddAvailableItemsToCartAsync(userId);
        return HandleResult(result);
    }
}

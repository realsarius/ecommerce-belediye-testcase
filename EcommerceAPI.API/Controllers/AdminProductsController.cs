using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = "Admin,Seller")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ISellerProfileService _sellerProfileService;

    public AdminProductsController(
        IProductService productService,
        ISellerProfileService sellerProfileService)
    {
        _productService = productService;
        _sellerProfileService = sellerProfileService;
    }

    private (int? UserId, string? Role) GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        
        int? userId = null;
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
            userId = id;
            
        return (userId, roleClaim?.Value);
    }

    /// <summary>
    /// Seller ise kendi ürünlerini, Admin ise tüm ürünleri listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductListRequest request)
    {
        var (userId, role) = GetCurrentUser();
        
        if (role == "Seller" && userId.HasValue)
        {
            // Seller'ın SellerProfile ID'sini al
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return Ok(new { data = new PaginatedResponse<ProductDto> { Items = new List<ProductDto>(), TotalCount = 0 } });
            
            var result = await _productService.GetProductsForSellerAsync(request, profileResult.Data.Id);
            return Ok(result);
        }
        
        // Admin tüm ürünleri görür
        var allProducts = await _productService.GetProductsAsync(request);
        return Ok(allProducts);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var (userId, role) = GetCurrentUser();
        int? sellerId = null;
        
        // Seller ise SellerId ata
        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest(new { message = "Önce satıcı profilinizi oluşturmanız gerekiyor" });
                
            sellerId = profileResult.Data.Id;
        }
        
        var result = await _productService.CreateProductAsync(request, sellerId);
        
        if (result.Success)
        {
            return CreatedAtRoute(
                routeName: "GetProductById",
                routeValues: new { id = result.Data.Id },
                value: result);
        }
        return BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var (userId, role) = GetCurrentUser();
        int? sellerId = null;
        
        // Seller ise ownership kontrolü için sellerId geç
        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest(new { message = "Satıcı profiliniz bulunamadı" });
                
            sellerId = profileResult.Data.Id;
        }
        
        var result = await _productService.UpdateProductAsync(id, request, sellerId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var (userId, role) = GetCurrentUser();
        int? sellerId = null;
        
        // Seller ise ownership kontrolü için sellerId geç
        if (role == "Seller" && userId.HasValue)
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest(new { message = "Satıcı profiliniz bulunamadı" });
                
            sellerId = profileResult.Data.Id;
        }
        
        var result = await _productService.DeleteProductAsync(id, sellerId);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }

    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var (userId, role) = GetCurrentUser();
        
        if (!userId.HasValue)
            return Unauthorized(new { message = "Geçersiz kullanıcı oturumu" });

        // Seller ise kendi ürününün stoğunu güncelleyebilir
        if (role == "Seller")
        {
            var profileResult = await _sellerProfileService.GetByUserIdAsync(userId.Value);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest(new { message = "Satıcı profiliniz bulunamadı" });
            
            var isOwner = await _productService.IsProductOwnedBySellerAsync(id, profileResult.Data.Id);
            if (!isOwner)
                return BadRequest(new { message = "Bu ürünün stoğunu güncelleme yetkiniz yok" });
        }

        var result = await _productService.UpdateStockAsync(id, request, userId.Value);
        
        if (result.Success)
        {
            return Ok(result);
        }
        return BadRequest(result);
    }
}

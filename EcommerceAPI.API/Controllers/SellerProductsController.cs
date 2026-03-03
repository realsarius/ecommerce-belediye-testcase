using System.Security.Claims;
using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/products")]
[Authorize(Roles = "Seller")]
public class SellerProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly ISellerProfileService _sellerProfileService;

    public SellerProductsController(IProductService productService, ISellerProfileService sellerProfileService)
    {
        _productService = productService;
        _sellerProfileService = sellerProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductListRequest request)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profili bulunamadı." });
        }

        var result = await _productService.GetProductsForSellerAsync(request, sellerId.Value);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profili bulunamadı." });
        }

        var result = await _productService.GetProductForSellerAsync(id, sellerId.Value);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Önce satıcı profilinizi oluşturmanız gerekiyor." });
        }

        var result = await _productService.CreateProductAsync(request, sellerId.Value);
        return HandleCreatedResult(result, nameof(GetProduct), new { id = result.Data?.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profiliniz bulunamadı." });
        }

        var result = await _productService.UpdateProductAsync(id, request, sellerId.Value);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profiliniz bulunamadı." });
        }

        var result = await _productService.DeleteProductAsync(id, sellerId.Value);
        return HandleResult(result);
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { success = false, message = "Geçersiz kullanıcı oturumu." });
        }

        var sellerId = await ResolveSellerIdAsync();
        if (sellerId == null)
        {
            return BadRequest(new { success = false, message = "Satıcı profiliniz bulunamadı." });
        }

        var isOwner = await _productService.IsProductOwnedBySellerAsync(id, sellerId.Value);
        if (!isOwner)
        {
            return BadRequest(new { success = false, message = "Bu ürünün stoğunu güncelleme yetkiniz yok." });
        }

        var result = await _productService.UpdateStockAsync(id, request, userId);
        return HandleResult(result);
    }

    private async Task<int?> ResolveSellerIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var profileResult = await _sellerProfileService.GetByUserIdAsync(userId);
        return profileResult.Success ? profileResult.Data?.Id : null;
    }
}

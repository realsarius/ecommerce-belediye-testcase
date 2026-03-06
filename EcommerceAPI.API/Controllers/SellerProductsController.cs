using EcommerceAPI.Application.Abstractions.ServiceContracts;
using EcommerceAPI.Entities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/v1/seller/products")]
[Authorize(Roles = "Seller")]
public class SellerProductsController : SellerApiControllerBase
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
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _productService.GetProductsForSellerAsync(request, sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile();
        }

        var result = await _productService.GetProductForSellerAsync(id, sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile("Önce satıcı profilinizi oluşturmanız gerekiyor.");
        }

        var result = await _productService.CreateProductAsync(request, sellerContext.SellerProfileId.Value);
        return HandleCreatedResult(result, nameof(GetProduct), new { id = result.Data?.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile("Satıcı profiliniz bulunamadı.");
        }

        var result = await _productService.UpdateProductAsync(id, request, sellerContext.SellerProfileId.Value);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile("Satıcı profiliniz bulunamadı.");
        }

        var result = await _productService.DeleteProductAsync(id, sellerContext.SellerProfileId.Value);
        return HandleDeleteResult(result);
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var sellerContext = await GetSellerContextAsync(_sellerProfileService);
        if (sellerContext == null)
        {
            return InvalidSellerSession();
        }
        if (sellerContext.SellerProfileId == null)
        {
            return MissingSellerProfile("Satıcı profiliniz bulunamadı.");
        }

        var isOwner = await _productService.IsProductOwnedBySellerAsync(id, sellerContext.SellerProfileId.Value);
        if (!isOwner)
        {
            return HandleForbidden("Bu ürünün stoğunu güncelleme yetkiniz yok.");
        }

        var result = await _productService.UpdateStockAsync(id, request, sellerContext.UserId);
        return HandleResult(result);
    }
}

using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceAPI.API.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public AdminProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request);
        return CreatedAtRoute(
            routeName: "GetProductById",
            routeValues: new { id = product.Id },
            value: product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.UpdateProductAsync(id, request);
        
        if (product == null)
            return NotFound(new { message = "Product not found" });

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _productService.DeleteProductAsync(id);
        
        if (!result)
            return NotFound(new { message = "Product not found" });

        return NoContent();
    }

    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        var result = await _productService.UpdateStockAsync(id, request, userId);
        
        if (!result)
            return BadRequest(new { message = "Stock update failed. Product not found or insufficient stock." });

        return Ok(new { message = "Stock updated successfully" });
    }
}

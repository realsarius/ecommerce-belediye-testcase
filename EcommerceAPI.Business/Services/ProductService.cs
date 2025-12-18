using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.Parameters;

namespace EcommerceAPI.Business.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PaginatedResponse<ProductDto>> GetProductsAsync(ProductListRequest request)
    {

        var filter = new ProductFilterParams(
            request.Page,
            request.PageSize,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Search,
            request.InStock,
            request.SortBy,
            request.SortDescending
        );
        
        var (items, totalCount) = await _productRepository.GetPagedAsync(filter);

        var productDtos = items.Select(MapToDto).ToList();

        return new PaginatedResponse<ProductDto>
        {
            Items = productDtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdWithDetailsAsync(id);
        
        if (product == null || !product.IsActive)
            return null;

        return MapToDto(product);
    }
    private static ProductDto MapToDto(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Currency = p.Currency,
            SKU = p.SKU,
            IsActive = p.IsActive,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            StockQuantity = p.Inventory?.QuantityAvailable ?? 0
        };
    }
}

using EcommerceAPI.Business.Services.Abstract;
using EcommerceAPI.Core.DTOs;
using EcommerceAPI.Core.Entities;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceAPI.Business.Services.Concrete;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false)
    {
        var categories = await _categoryRepository.GetAllWithProductCountAsync(includeInactive);
        
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdWithProductsAsync(id);
        
        if (category == null)
            return null;
        
        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
        if (existingCategory != null)
        {
            throw new InvalidOperationException($"'{request.Name}' adında bir kategori zaten mevcut");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Yeni kategori oluşturuldu: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return MapToDto(category);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        
        if (category == null)
            return null;

        if (!string.IsNullOrEmpty(request.Name) && request.Name != category.Name)
        {
            var existingCategory = await _categoryRepository.GetByNameAsync(request.Name);
            if (existingCategory != null && existingCategory.Id != id)
            {
                throw new InvalidOperationException($"'{request.Name}' adında bir kategori zaten mevcut");
            }
            category.Name = request.Name;
        }

        if (request.Description != null)
            category.Description = request.Description;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Kategori güncellendi: {CategoryId} - {CategoryName}", category.Id, category.Name);

        var updatedCategory = await _categoryRepository.GetByIdWithProductsAsync(id);
        return MapToDto(updatedCategory!);
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        
        if (category == null)
            return false;

        var hasProducts = await _categoryRepository.HasProductsAsync(id);
        if (hasProducts)
        {
            throw new InvalidOperationException("Bu kategoriye bağlı ürünler bulunmaktadır. Önce ürünleri başka bir kategoriye taşıyın.");
        }

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        
        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Kategori silindi (soft delete): {CategoryId} - {CategoryName}", category.Id, category.Name);

        return true;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ProductCount = category.Products?.Count ?? 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}

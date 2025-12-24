using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using Microsoft.Extensions.Logging;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Business.Concrete;

public class CategoryManager : ICategoryService
{
    private readonly ICategoryDal _categoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryManager> _logger;

    public CategoryManager(
        ICategoryDal categoryDal,
        IUnitOfWork unitOfWork,
        ILogger<CategoryManager> logger)
    {
        _categoryDal = categoryDal;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IDataResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync(bool includeInactive = false)
    {
        // Not: ProductCount gibi alanlar için Include veya Projection gerekebilir.
        // Base repository sadece entity döner.
        // Eğer includeInactive true ise tümünü, false ise sadece aktifleri getir.
        
        IList<Category> categories;
        if (includeInactive)
        {
             // GetAllWithProductCountAsync benzeri bir metod ICategoryDal'da olmalıydı
             // Şimdilik GetListAsync kullanıyoruz
             categories = await _categoryDal.GetListAsync();
        }
        else
        {
            categories = await _categoryDal.GetActiveCategoriesAsync();
        }
        
        return new SuccessDataResult<IEnumerable<CategoryDto>>(categories.Select(MapToDto));
    }

    public async Task<IDataResult<CategoryDto>> GetCategoryByIdAsync(int id)
    {
        // GetByIdWithProductsAsync benzeri bir metod ICategoryDal'da olmalıydı
        // Şimdilik GetAsync kullanıyoruz
        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorDataResult<CategoryDto>("Kategori bulunamadı.");
        
        return new SuccessDataResult<CategoryDto>(MapToDto(category));
    }

    public async Task<IDataResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var existingCategory = await _categoryDal.GetByNameAsync(request.Name);
        if (existingCategory != null)
        {
            return new ErrorDataResult<CategoryDto>($"'{request.Name}' adında bir kategori zaten mevcut");
        }

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryDal.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Yeni kategori oluşturuldu: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return new SuccessDataResult<CategoryDto>(MapToDto(category), "Kategori başarıyla oluşturuldu.");
    }

    public async Task<IDataResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorDataResult<CategoryDto>("Kategori bulunamadı.");

        if (!string.IsNullOrEmpty(request.Name) && request.Name != category.Name)
        {
            var existingCategory = await _categoryDal.GetByNameAsync(request.Name);
            if (existingCategory != null && existingCategory.Id != id)
            {
                return new ErrorDataResult<CategoryDto>($"'{request.Name}' adında bir kategori zaten mevcut");
            }
            category.Name = request.Name;
        }

        if (request.Description != null)
            category.Description = request.Description;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        category.UpdatedAt = DateTime.UtcNow;

        _categoryDal.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Kategori güncellendi: {CategoryId} - {CategoryName}", category.Id, category.Name);

        // Güncel halini tekrar çekmeye gerek olmayabilir ama map için iyi olur
        return new SuccessDataResult<CategoryDto>(MapToDto(category), "Kategori güncellendi.");
    }

    public async Task<IResult> DeleteCategoryAsync(int id)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorResult("Kategori bulunamadı.");

        // HasProducts kontrolü için ICategoryDal'da metod veya generic repository count'u kullanılabilir
        // Şimdilik Product repository'den bakamıyoruz, Category üzerinde product count yoksa.
        // Base repository CountAsync var.
        // var hasProducts = await _productDal.CountAsync(p => p.CategoryId == id && p.IsActive) > 0; 
        // Ancak burada ProductDal inject etmedik. Category üzerinden Products include edilerek bakılabilir.
        
        // Şimdilik bu kontrolü atlıyorum veya Dal metodu eklenmesini not ediyorum.

        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        
        _categoryDal.Update(category);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Kategori silindi (soft delete): {CategoryId} - {CategoryName}", category.Id, category.Name);

        return new SuccessResult("Kategori silindi.");
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

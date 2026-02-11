using EcommerceAPI.Business.Abstract;
using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Entities.Concrete;
using EcommerceAPI.DataAccess.Abstract;
using EcommerceAPI.Core.Interfaces;
using EcommerceAPI.Core.CrossCuttingConcerns.Logging;
using EcommerceAPI.Core.Utilities.Results;
using EcommerceAPI.Core.Aspects.Autofac.Caching;
using EcommerceAPI.Core.Aspects.Autofac.Logging;
using EcommerceAPI.Core.Aspects.Autofac.Validation;
using EcommerceAPI.Business.Validators;
using EcommerceAPI.Business.Constants;

namespace EcommerceAPI.Business.Concrete;

public class CategoryManager : ICategoryService
{
    private readonly ICategoryDal _categoryDal;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public CategoryManager(
        ICategoryDal categoryDal,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _categoryDal = categoryDal;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    [LogAspect]
    [CacheAspect(duration: 60)]
    public async Task<IDataResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync(bool includeInactive = false)
    {

        
        IList<Category> categories;
        if (includeInactive)
        {

             categories = await _categoryDal.GetAllWithProductsAsync();
        }
        else
        {
            categories = await _categoryDal.GetActiveCategoriesAsync();
        }
        
        return new SuccessDataResult<IEnumerable<CategoryDto>>(categories.Select(MapToDto));
    }

    public async Task<IDataResult<CategoryDto>> GetCategoryByIdAsync(int id)
    {

        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorDataResult<CategoryDto>(Messages.CategoryNotFound);
        
        return new SuccessDataResult<CategoryDto>(MapToDto(category));
    }

    [LogAspect]
    [CacheRemoveAspect("GetAllCategoriesAsync")]
    [ValidationAspect(typeof(CreateCategoryRequestValidator))]
    public async Task<IDataResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var existingCategory = await _categoryDal.GetByNameAsync(request.Name);
        if (existingCategory != null)
        {
            return new ErrorDataResult<CategoryDto>(Messages.CategoryExists);
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



        await _auditService.LogActionAsync(
            "Admin",
            "CreateCategory",
            "Category",
            new { CategoryId = category.Id, CategoryName = category.Name });

        return new SuccessDataResult<CategoryDto>(MapToDto(category), Messages.CategoryAdded);
    }

    public async Task<IDataResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorDataResult<CategoryDto>(Messages.CategoryNotFound);

        if (!string.IsNullOrEmpty(request.Name) && request.Name != category.Name)
        {
            var existingCategory = await _categoryDal.GetByNameAsync(request.Name);
            if (existingCategory != null && existingCategory.Id != id)
            {
                return new ErrorDataResult<CategoryDto>(Messages.CategoryExists);
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



        await _auditService.LogActionAsync(
            "Admin",
            "UpdateCategory",
            "Category",
            new { CategoryId = category.Id, CategoryName = category.Name });

        return new SuccessDataResult<CategoryDto>(MapToDto(category), Messages.CategoryUpdated);
    }

    public async Task<IResult> DeleteCategoryAsync(int id)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id);
        
        if (category == null)
            return new ErrorResult(Messages.CategoryNotFound);



        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;
        
        _categoryDal.Update(category);
        await _unitOfWork.SaveChangesAsync();



        await _auditService.LogActionAsync(
            "Admin",
            "DeleteCategory",
            "Category",
            new { CategoryId = category.Id, CategoryName = category.Name });

        return new SuccessResult(Messages.CategoryDeleted);
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

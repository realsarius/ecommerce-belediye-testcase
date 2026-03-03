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
        IList<Category> categories = await _categoryDal.GetAllWithHierarchyAsync(includeInactive);
        
        return new SuccessDataResult<IEnumerable<CategoryDto>>(categories.Select(MapToDto).ToList());
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
            IsActive = request.IsActive,
            ParentCategoryId = request.ParentCategoryId,
            SortOrder = await ResolveNextSortOrderAsync(request.ParentCategoryId),
            CreatedAt = DateTime.UtcNow
        };

        if (request.ParentCategoryId.HasValue)
        {
            var parentCategory = await _categoryDal.GetAsync(c => c.Id == request.ParentCategoryId.Value);
            if (parentCategory == null)
            {
                return new ErrorDataResult<CategoryDto>(Messages.CategoryNotFound);
            }
        }

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

        if (request.ParentCategoryId.HasValue && request.ParentCategoryId.Value == id)
        {
            return new ErrorDataResult<CategoryDto>("Kategori kendi üst kategorisi olamaz.");
        }

        if (request.ParentCategoryId != category.ParentCategoryId)
        {
            if (request.ParentCategoryId.HasValue)
            {
                var categories = await _categoryDal.GetAllWithHierarchyAsync(includeInactive: true);
                var parent = categories.FirstOrDefault(c => c.Id == request.ParentCategoryId.Value);
                if (parent == null)
                {
                    return new ErrorDataResult<CategoryDto>(Messages.CategoryNotFound);
                }

                if (WouldCreateCircularReference(id, request.ParentCategoryId.Value, categories))
                {
                    return new ErrorDataResult<CategoryDto>("Kategori döngüsel bir hiyerarşiye taşınamaz.");
                }
            }

            category.ParentCategoryId = request.ParentCategoryId;
            category.SortOrder = await ResolveNextSortOrderAsync(request.ParentCategoryId, excludeCategoryId: id);
        }

        if (request.SortOrder.HasValue)
        {
            category.SortOrder = request.SortOrder.Value;
        }

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

    public async Task<IResult> ReorderCategoriesAsync(ReorderCategoriesRequest request)
    {
        if (request.Items.Count == 0)
        {
            return new ErrorResult("Sıralanacak kategori bulunamadı.");
        }

        var categories = await _categoryDal.GetAllWithHierarchyAsync(includeInactive: true);
        var categoryMap = categories.ToDictionary(c => c.Id);

        foreach (var item in request.Items)
        {
            if (!categoryMap.TryGetValue(item.Id, out var category))
            {
                return new ErrorResult(Messages.CategoryNotFound);
            }

            if (item.ParentCategoryId == item.Id)
            {
                return new ErrorResult("Kategori kendi üst kategorisi olamaz.");
            }

            if (item.ParentCategoryId.HasValue)
            {
                if (!categoryMap.ContainsKey(item.ParentCategoryId.Value))
                {
                    return new ErrorResult(Messages.CategoryNotFound);
                }

                if (WouldCreateCircularReference(item.Id, item.ParentCategoryId.Value, categories))
                {
                    return new ErrorResult("Kategori döngüsel bir hiyerarşiye taşınamaz.");
                }
            }

            category.ParentCategoryId = item.ParentCategoryId;
            category.SortOrder = item.SortOrder;
            category.UpdatedAt = DateTime.UtcNow;
            _categoryDal.Update(category);
        }

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogActionAsync(
            "Admin",
            "ReorderCategories",
            "Category",
            new { CategoryCount = request.Items.Count });

        return new SuccessResult("Kategori sıralaması güncellendi.");
    }

    public async Task<IResult> DeleteCategoryAsync(int id)
    {
        var category = (await _categoryDal.GetAllWithHierarchyAsync(includeInactive: true)).FirstOrDefault(c => c.Id == id);
        
        if (category == null)
            return new ErrorResult(Messages.CategoryNotFound);

        if (category.Products.Count > 0)
        {
            return new ErrorResult("Bu kategoriye bağlı ürünler olduğu için silinemez.");
        }

        if (category.Children.Any(child => child.IsActive))
        {
            return new ErrorResult("Alt kategorileri bulunan bir kategori silinemez.");
        }

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
            ParentCategoryId = category.ParentCategoryId,
            SortOrder = category.SortOrder,
            ProductCount = category.Products?.Count ?? 0,
            ChildCount = category.Children.Count,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    private async Task<int> ResolveNextSortOrderAsync(int? parentCategoryId, int? excludeCategoryId = null)
    {
        var categories = await _categoryDal.GetAllWithHierarchyAsync(includeInactive: true);

        return categories
            .Where(c => c.ParentCategoryId == parentCategoryId && (!excludeCategoryId.HasValue || c.Id != excludeCategoryId.Value))
            .Select(c => c.SortOrder)
            .DefaultIfEmpty(-1)
            .Max() + 1;
    }

    private static bool WouldCreateCircularReference(int categoryId, int parentCategoryId, IEnumerable<Category> categories)
    {
        var categoryMap = categories.ToDictionary(c => c.Id);
        var currentParentId = parentCategoryId;

        while (categoryMap.TryGetValue(currentParentId, out var parentCategory))
        {
            if (parentCategory.Id == categoryId)
            {
                return true;
            }

            if (!parentCategory.ParentCategoryId.HasValue)
            {
                return false;
            }

            currentParentId = parentCategory.ParentCategoryId.Value;
        }

        return false;
    }
}

using EcommerceAPI.Core.DTOs;

namespace EcommerceAPI.Business.Services.Abstract;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false);
    
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
    
    Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
    
    Task<bool> DeleteCategoryAsync(int id);
}

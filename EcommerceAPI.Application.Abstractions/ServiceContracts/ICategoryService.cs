using EcommerceAPI.Entities.DTOs;
using EcommerceAPI.Core.Utilities.Results;

namespace EcommerceAPI.Application.Abstractions.ServiceContracts;

public interface ICategoryService
{
    Task<IDataResult<IEnumerable<CategoryDto>>> GetAllCategoriesAsync(bool includeInactive = false);
    
    Task<IDataResult<CategoryDto>> GetCategoryByIdAsync(int id);
    
    Task<IDataResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request);
    
    Task<IDataResult<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request);

    Task<IResult> ReorderCategoriesAsync(ReorderCategoriesRequest request);
    
    Task<IResult> DeleteCategoryAsync(int id);
}

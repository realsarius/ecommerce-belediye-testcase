using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.Application.Abstractions.Persistence;

public interface ICategoryDal : IEntityRepository<Category>
{
    Task<Category?> GetByNameAsync(string name);
    Task<IList<Category>> GetActiveCategoriesAsync();
    Task<IList<Category>> GetAllWithProductsAsync();
    Task<IList<Category>> GetAllWithHierarchyAsync(bool includeInactive = false);
    Task<int> GetDashboardCategoryCountAsync();
}

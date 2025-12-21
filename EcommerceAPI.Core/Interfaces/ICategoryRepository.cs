using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByNameAsync(string name);
    Task<Category?> GetByIdWithProductsAsync(int id);
    Task<IEnumerable<Category>> GetAllWithProductCountAsync(bool includeInactive = false);
    Task<bool> HasProductsAsync(int categoryId);
}

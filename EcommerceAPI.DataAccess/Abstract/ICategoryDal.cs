using EcommerceAPI.Core.DataAccess;
using EcommerceAPI.Entities.Concrete;

namespace EcommerceAPI.DataAccess.Abstract;

public interface ICategoryDal : IEntityRepository<Category>
{
    Task<Category?> GetByNameAsync(string name);
    Task<IList<Category>> GetActiveCategoriesAsync();
    Task<IList<Category>> GetAllWithProductsAsync();
}

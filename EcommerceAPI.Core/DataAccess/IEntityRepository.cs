using System.Linq.Expressions;
using EcommerceAPI.Core.Entities;

namespace EcommerceAPI.Core.DataAccess;

/// <summary>
/// Generic repository interface.
/// Tüm entity repository'leri bu interface'i extend eder.
/// </summary>
public interface IEntityRepository<T> where T : class, IEntity, new()
{
    Task<T?> GetAsync(Expression<Func<T, bool>> filter);
    Task<IList<T>> GetListAsync(Expression<Func<T, bool>>? filter = null);
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
}

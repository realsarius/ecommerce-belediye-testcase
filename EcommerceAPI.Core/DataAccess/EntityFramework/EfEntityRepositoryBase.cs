using System.Linq.Expressions;
using EcommerceAPI.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Core.DataAccess.EntityFramework;

/// <summary>
/// Generic Entity Framework repository base class.
/// Tüm EF repository'leri bu sınıftan türer.
/// </summary>
public class EfEntityRepositoryBase<TEntity, TContext> : IEntityRepository<TEntity>
    where TEntity : class, IEntity, new()
    where TContext : DbContext
{
    protected readonly TContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public EfEntityRepositoryBase(TContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(filter);
    }

    public async Task<IList<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return filter == null
            ? await _dbSet.AsNoTracking().ToListAsync()
            : await _dbSet.AsNoTracking().Where(filter).ToListAsync();
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet.AnyAsync(filter);
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
    {
        return filter == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(filter);
    }
}

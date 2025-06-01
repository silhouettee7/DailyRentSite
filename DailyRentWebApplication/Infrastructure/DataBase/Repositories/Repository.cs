using System.Linq.Expressions;
using Domain.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataBase.Repositories;

public abstract class Repository<TEntity>: 
    IRepository<TEntity> where TEntity : class
{
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly AppDbContext context;

    protected Repository(AppDbContext context)
    {
        this.context = context;
        _dbSet = this.context.Set<TEntity>();
    }
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual TEntity Update(TEntity entity)
    {
        _dbSet.Update(entity);
        return entity;
    }

    public virtual TEntity PatchUpdate(TEntity entity, Action<TEntity> patchAction)
    {
        context.Attach(entity);
        patchAction(entity);
        return entity;
    }

    public virtual void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual async Task<IEnumerable<TEntity?>> GetAllByFilterAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(filter)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public virtual async Task<TEntity?> GetByFilterAsync(Expression<Func<TEntity, bool>> filter)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(filter);
    }
}
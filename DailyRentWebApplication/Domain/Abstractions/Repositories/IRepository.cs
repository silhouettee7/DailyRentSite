using System.Linq.Expressions;

namespace Domain.Abstractions.Repositories;

public interface IRepository<T> where T : class
{
    Task<T> AddAsync(T entity);
    T Update(T entity);
    T PatchUpdate(T entity, Action<T> patchAction);
    void Delete(T entity);
    Task<T?> GetByFilterAsync(Expression<Func<T, bool>> filter);
    Task<IEnumerable<T?>> GetAllByFilterAsync(Expression<Func<T, bool>> filter);
    Task SaveChangesAsync();
}
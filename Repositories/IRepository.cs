using System.Linq.Expressions;

namespace FlightApp.Repositories
{
    // Minimal generic repo used by specialized repos/services
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(params object[] keys);
        Task<List<T>> ListAsync(Expression<Func<T, bool>>? filter = null);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(params object[] keys);

        // Add the missing method definition to fix the error  
        Task<List<T>> GetAllAsync();
    }
}

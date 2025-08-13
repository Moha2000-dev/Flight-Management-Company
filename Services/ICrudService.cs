using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightApp.Services
{
    public interface ICrudService<T> where T : class
    {
        Task<T> GetAsync(int id);
        Task<List<T>> AllAsync();
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}

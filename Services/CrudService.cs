using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class CrudService<T> : ICrudService<T> where T : class
    {
        protected readonly IRepository<T> _repo;
        public CrudService(IRepository<T> repo) { _repo = repo; }

        public Task<T> GetAsync(int id) => _repo.GetByIdAsync(id);
        public Task<List<T>> AllAsync() => _repo.GetAllAsync();
        public Task<T> CreateAsync(T entity) => _repo.AddAsync(entity);
        public Task UpdateAsync(T entity) => _repo.UpdateAsync(entity);
        public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}

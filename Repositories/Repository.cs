using System.Linq.Expressions;
using FlightApp.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly FlightDbContext _db;
        protected readonly DbSet<T> _set;

        public Repository(FlightDbContext db)
        {
            _db = db;
            _set = _db.Set<T>();
        }

        public Task<T?> GetByIdAsync(params object[] keys) => _set.FindAsync(keys).AsTask();

        public Task<List<T>> ListAsync(Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> q = _set.AsNoTracking();
            if (filter != null) q = q.Where(filter);
            return q.ToListAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            _set.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(params object[] keys)
        {
            var e = await _set.FindAsync(keys);
            if (e != null) { _set.Remove(e); await _db.SaveChangesAsync(); }
        }
    }
}

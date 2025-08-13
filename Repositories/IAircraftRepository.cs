using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IAircraftRepository : IRepository<Aircraft>
    {
        Task<Aircraft?> GetByTailAsync(string tailNumber);
    }
}
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IAircraftService : ICrudService<Aircraft>
    {
        Task<Aircraft?> GetByTailAsync(string tailNumber);
    }
}
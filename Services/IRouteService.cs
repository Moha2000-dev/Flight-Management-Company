using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IRouteService : ICrudService<Route>
    {
        Task<Route?> FindByIatasAsync(string originIata, string destIata);
    }
}

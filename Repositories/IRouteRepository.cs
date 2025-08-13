using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IRouteRepository : IRepository<Route>
    {
        Task<Route?> FindByIatasAsync(string originIata, string destIata);
    }
}

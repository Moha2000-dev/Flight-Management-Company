using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class RouteService : CrudService<Route>, IRouteService
    {
        private readonly IRouteRepository _routes;
        public RouteService(IRouteRepository routes) : base(routes) { _routes = routes; }
        public Task<Route?> FindByIatasAsync(string originIata, string destIata) =>
            _routes.FindByIatasAsync(originIata, destIata);
    }
}
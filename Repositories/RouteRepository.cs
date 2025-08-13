using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class RouteRepository : Repository<Route>, IRouteRepository
    {
        public RouteRepository(FlightDbContext db) : base(db) { }

        public Task<Route?> FindByIatasAsync(string originIata, string destIata) =>
            _db.Routes
               .Include(r => r.OriginAirport)
               .Include(r => r.DestinationAirport)
               .FirstOrDefaultAsync(r => r.OriginAirport!.IATA == originIata
                                      && r.DestinationAirport!.IATA == destIata);
    }
}

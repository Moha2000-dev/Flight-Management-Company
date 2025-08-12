using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.DTOs;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class FlightRepository : Repository<Flight>, IFlightRepository
    {
        public FlightRepository(FlightDbContext db) : base(db) { }

        public async Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req)
        {
            var q = _db.Flights
                .Include(f => f.Route)!.ThenInclude(r => r!.OriginAirport)
                .Include(f => f.Route)!.ThenInclude(r => r!.DestinationAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Tickets)
                .AsQueryable();

            if (req.FromUtc.HasValue) q = q.Where(f => f.DepartureUtc >= req.FromUtc);
            if (req.ToUtc.HasValue) q = q.Where(f => f.DepartureUtc <= req.ToUtc);
            if (!string.IsNullOrWhiteSpace(req.OriginIata))
                q = q.Where(f => f.Route!.OriginAirport!.IATA == req.OriginIata!.ToUpper());
            if (!string.IsNullOrWhiteSpace(req.DestIata))
                q = q.Where(f => f.Route!.DestinationAirport!.IATA == req.DestIata!.ToUpper());

            var proj = q.Select(f => new FlightSearchDto(
                f.FlightId,
                f.FlightNumber,
                f.Route!.OriginAirport!.IATA,
                f.Route!.DestinationAirport!.IATA,
                f.DepartureUtc,
                f.ArrivalUtc,
                f.Aircraft != null ? f.Aircraft.Model : string.Empty,
                f.Aircraft != null ? f.Aircraft.Capacity : 0,
                f.Tickets.Count,
                f.Tickets.Select(t => (decimal?)t.Fare).Min() ?? 0m
            ));

            if (req.MinFare.HasValue) proj = proj.Where(x => x.MinFare >= req.MinFare);
            if (req.MaxFare.HasValue) proj = proj.Where(x => x.MinFare <= req.MaxFare);

            return await proj.OrderBy(x => x.DepartureUtc)
                             .AsNoTracking()
                             .ToListAsync();
        }
    }
}

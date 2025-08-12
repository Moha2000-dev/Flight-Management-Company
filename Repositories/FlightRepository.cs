using FlightApp.Data;
using FlightApp.DTOs;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    // Custom queries for Flight (search + price filter)
    public class FlightRepository : Repository<Flight>
    {
        public FlightRepository(FlightDbContext db) : base(db) {}

        public async Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req)
        {
            // Pre-aggregate tickets per flight (seats + min fare).
            // Make them nullable so the LEFT JOIN works when there are no tickets yet.
            var ticketAgg =
                from t in _db.Tickets
                group t by t.FlightId into g
                select new
                {
                    FlightId = g.Key,
                    SeatsSold = (int?)g.Count(),
                    MinFare = (decimal?)g.Min(x => x.Fare)
                };

            // Core query using explicit joins (no Include/Select mix = simpler SQL)
            var q =
                from f in _db.Flights
                join r in _db.Routes on f.RouteId equals r.RouteId
                join ao in _db.Airports on r.OriginAirportId equals ao.AirportId
                join ad in _db.Airports on r.DestinationAirportId equals ad.AirportId
                join a in _db.Aircraft on f.AircraftId equals a.AircraftId
                join ta in ticketAgg on f.FlightId equals ta.FlightId into tag
                from ta in tag.DefaultIfEmpty() // LEFT JOIN (flights with no tickets yet)
                select new
                {
                    f.FlightId,
                    f.FlightNumber,
                    OriginIata = ao.IATA,
                    DestIata = ad.IATA,
                    f.DepartureUtc,
                    f.ArrivalUtc,
                    AircraftModel = a.Model,
                    Capacity = a.Capacity,
                    SeatsSold = ta.SeatsSold ?? 0,
                    MinFare = ta.MinFare ?? 0m
                };

            // Filters
            if (req.FromUtc is not null) q = q.Where(x => x.DepartureUtc >= req.FromUtc);
            if (req.ToUtc is not null) q = q.Where(x => x.DepartureUtc <= req.ToUtc);
            if (!string.IsNullOrWhiteSpace(req.OriginIata))
                q = q.Where(x => x.OriginIata == req.OriginIata);
            if (!string.IsNullOrWhiteSpace(req.DestIata))
                q = q.Where(x => x.DestIata == req.DestIata);
            if (req.MinFare is not null) q = q.Where(x => x.MinFare >= req.MinFare);
            if (req.MaxFare is not null) q = q.Where(x => x.MinFare <= req.MaxFare);

            // Project to DTO at the end (keeps SQL simpler)
            return await q
                .OrderBy(x => x.DepartureUtc)
                .Select(x => new FlightSearchDto(
                    x.FlightId,
                    x.FlightNumber,
                    x.OriginIata,
                    x.DestIata,
                    x.DepartureUtc,
                    x.ArrivalUtc,
                    x.AircraftModel,
                    x.Capacity,
                    x.SeatsSold,
                    x.MinFare))
                .AsNoTracking()
                .ToListAsync();
        }


    }
}

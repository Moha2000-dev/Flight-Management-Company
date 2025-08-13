using System;
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

        // -------- Search (unchanged) --------
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

        // -------- Reports / helpers --------

        public async Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc)
        {
            var start = dayUtc.Date;
            var end = start.AddDays(1);

            return await _db.Flights
                .Where(f => f.DepartureUtc >= start && f.DepartureUtc < end)
                .Select(f => new FlightManifestDto(
                    f.FlightId,
                    f.FlightNumber,
                    f.Route!.OriginAirport!.IATA,
                    f.Route!.DestinationAirport!.IATA,
                    f.DepartureUtc,
                    f.Tickets.Count))
                .OrderBy(f => f.DepartureUtc)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(
            DateTime fromUtc, DateTime toUtc, int topN)
        {
            return await _db.Tickets
                .Where(t => t.Flight!.DepartureUtc >= fromUtc && t.Flight.DepartureUtc <= toUtc)
                .GroupBy(t => new
                {
                    O = t.Flight!.Route!.OriginAirport!.IATA,
                    D = t.Flight!.Route!.DestinationAirport!.IATA
                })
                .Select(g => new RouteRevenueDto(
                    g.Key.O,
                    g.Key.D,
                    g.Select(x => x.FlightId).Distinct().Count(),
                    g.Count(),
                    g.Sum(x => x.Fare)))
                .OrderByDescending(r => r.Revenue)
                .Take(topN)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(
            DateTime fromUtc, DateTime toUtc, int minPercent)
        {
            return await _db.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.DepartureUtc >= fromUtc && f.DepartureUtc <= toUtc)
                .Select(f => new SeatOccupancyDto(
                    f.FlightId,
                    f.FlightNumber,
                    f.DepartureUtc,                            // << include DepartureUtc (matches DTO order)
                    f.Aircraft!.Capacity,
                    f.Tickets.Count,
                    f.Aircraft!.Capacity == 0
                        ? 0
                        : (int)Math.Round(100.0 * f.Tickets.Count / f.Aircraft!.Capacity)))
                .Where(x => x.Percent >= minPercent)
                .OrderByDescending(x => x.Percent)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId)
        {
            var f = await _db.Flights
                .Include(x => x.Aircraft)
                .FirstOrDefaultAsync(x => x.FlightId == flightId);
            if (f == null) return null;

            var sold = await _db.Tickets.CountAsync(t => t.FlightId == flightId);
            var cap = f.Aircraft?.Capacity ?? 0;
            var avail = Math.Max(cap - sold, 0);

            return new AvailableSeatsDto(f.FlightId, f.FlightNumber, cap, sold, avail);
        }
        public Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg)
        {
            return _db.Baggage
                .Where(b => b.WeightKg > thresholdKg)
                .Select(b => new OverweightBagDto(
                    b.Ticket!.Booking!.BookingRef,
                    b.TagNumber,
                    b.WeightKg,
                    b.Ticket.Flight!.FlightNumber,
                    b.Ticket.Flight.Route!.OriginAirport!.IATA,
                    b.Ticket.Flight.Route!.DestinationAirport!.IATA))
                .OrderByDescending(x => x.WeightKg)
                .AsNoTracking()
                .ToListAsync();
        }


    }
}

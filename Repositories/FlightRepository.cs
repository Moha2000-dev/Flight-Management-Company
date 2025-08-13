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

        // ---------------- Search ----------------
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

            return await proj
                .OrderBy(x => x.DepartureUtc)       // order BEFORE materializing – EF friendly
                .AsNoTracking()
                .ToListAsync();
        }

        // ---------------- Reports / helpers ----------------

        // 1) Manifest – order by entity field, then project (avoids OrderBy(new DTO).Prop)
        public async Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc)
        {
            var start = dayUtc.Date;
            var end = start.AddDays(1);

            return await _db.Flights
                .Where(f => f.DepartureUtc >= start && f.DepartureUtc < end)
                .OrderBy(f => f.DepartureUtc)
                .Select(f => new FlightManifestDto(
                    f.FlightId,
                    f.FlightNumber,
                    f.Route!.OriginAirport!.IATA,
                    f.Route!.DestinationAirport!.IATA,
                    f.DepartureUtc,
                    f.Tickets.Count))
                .AsNoTracking()
                .ToListAsync();
        }

        // 2) Top routes – pure GroupBy/Select (no AsQueryable)
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

        // 4) High occupancy – push the filter into EF math (don't filter on DTO.Percent)
        public async Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(
            DateTime fromUtc, DateTime toUtc, int minPercent)
        {
            return await _db.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.DepartureUtc >= fromUtc && f.DepartureUtc <= toUtc)
                .Where(f => f.Aircraft!.Capacity > 0 &&
                            (100.0 * f.Tickets.Count / f.Aircraft!.Capacity) >= minPercent)
                .OrderByDescending(f => (100.0 * f.Tickets.Count / f.Aircraft!.Capacity))
                .Select(f => new SeatOccupancyDto(
                    f.FlightId,
                    f.FlightNumber,
                    f.DepartureUtc,
                    f.Aircraft!.Capacity,
                    f.Tickets.Count,
                    (int)Math.Round(100.0 * f.Tickets.Count / f.Aircraft!.Capacity)))
                .AsNoTracking()
                .ToListAsync();
        }

        // 5) Available seats (summary)
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

        // 10) Overweight bags (simple)
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

        // 7) NEW – passengers with connections (same booking, sequential within N hours)
        public async Task<List<PassengerConnectionDto>> GetPassengersWithConnectionsAsync(int maxLayoverHours)
        {
            // Minimal columns for client-side pairing
            var legs = await _db.Tickets
                .Select(t => new
                {
                    t.BookingId,
                    t.Booking!.BookingRef,
                    PassengerName = t.Booking.Passenger!.FullName,
                    t.FlightId,
                    t.Flight!.FlightNumber,
                    t.Flight.DepartureUtc,
                    t.Flight.ArrivalUtc,
                    OriginIata = t.Flight.Route!.OriginAirport!.IATA,
                    DestIata = t.Flight.Route!.DestinationAirport!.IATA
                })
                .OrderBy(x => x.DepartureUtc)
                .AsNoTracking()
                .ToListAsync();

            var results = new List<PassengerConnectionDto>();
            foreach (var g in legs.GroupBy(x => x.BookingId))
            {
                var ordered = g.OrderBy(x => x.DepartureUtc).ToList();
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var a = ordered[i];
                    var b = ordered[i + 1];

                    // same day sequence, matching airport, layover within threshold
                    if (!string.Equals(a.DestIata, b.OriginIata, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var layover = (int)(b.DepartureUtc - a.ArrivalUtc).TotalMinutes;
                    if (layover < 0) continue; // overlapping in wrong order
                    if (layover > maxLayoverHours * 60) continue;

                    results.Add(new PassengerConnectionDto(
                        a.BookingRef,
                        a.PassengerName,
                        a.FlightNumber,
                        b.FlightNumber,
                        a.OriginIata,
                        a.DestIata,
                        b.DestIata,
                        a.DepartureUtc,
                        a.ArrivalUtc,
                        b.DepartureUtc,
                        layover));
                }
            }

            return results
                .OrderBy(r => r.PassengerName)
                .ThenBy(r => r.A_DepartureUtc)
                .ToList();
        }
    }
}

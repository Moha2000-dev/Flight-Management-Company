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

        // ---------- Search ----------
        public async Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req)
        {
            var q = _db.Flights
                .Include(f => f.Route)!.ThenInclude(r => r!.OriginAirport)
                .Include(f => f.Route)!.ThenInclude(r => r!.DestinationAirport)
                .Include(f => f.Aircraft)
                .Include(f => f.Tickets)
                .AsQueryable();

            if (req.FromUtc.HasValue) q = q.Where(f => f.DepartureUtc >= req.FromUtc.Value);
            if (req.ToUtc.HasValue) q = q.Where(f => f.DepartureUtc <= req.ToUtc.Value);
            if (!string.IsNullOrWhiteSpace(req.OriginIata))
                q = q.Where(f => f.Route!.OriginAirport!.IATA == req.OriginIata!.ToUpper());
            if (!string.IsNullOrWhiteSpace(req.DestIata))
                q = q.Where(f => f.Route!.DestinationAirport!.IATA == req.DestIata!.ToUpper());

            return await q
                .OrderBy(f => f.DepartureUtc)
                .Select(f => new FlightSearchDto(
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
                ))
                .Where(x => !req.MinFare.HasValue || x.MinFare >= req.MinFare.Value)
                .Where(x => !req.MaxFare.HasValue || x.MinFare <= req.MaxFare.Value)
                .AsNoTracking()
                .ToListAsync();
        }

        // ---------- Reports / helpers used by Reports menu ----------
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

        // FlightRepository.cs



        // DTO: RouteRevenueDto(string OriginIata, string DestIata, int Flights, int Tickets, decimal Revenue)

        public async Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(
            DateTime fromUtc, DateTime toUtc, int topN)
        {
            var flat = await _db.Tickets
                .Where(t => t.Flight!.DepartureUtc >= fromUtc &&
                            t.Flight.DepartureUtc <= toUtc)
                .Select(t => new
                {
                    t.FlightId,
                    t.Fare,
                    Origin = t.Flight!.Route!.OriginAirport!.IATA,
                    Dest = t.Flight!.Route!.DestinationAirport!.IATA
                })
                .AsNoTracking()
                .ToListAsync(); // <— materialize first

            return flat
                .GroupBy(x => new { x.Origin, x.Dest })
                .Select(g => new RouteRevenueDto(
                    g.Key.Origin,
                    g.Key.Dest,
                    g.Select(x => x.FlightId).Distinct().Count(),
                    g.Count(),
                    g.Sum(x => x.Fare)
                ))
                .OrderByDescending(r => r.Revenue)
                .Take(topN)
                .ToList();
        }




        public async Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent)
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
                .OrderByDescending(b => b.WeightKg)
                .Select(b => new OverweightBagDto(
                    b.Ticket!.Booking!.BookingRef,
                    b.TagNumber,
                    b.WeightKg,
                    b.Ticket.Flight!.FlightNumber,
                    b.Ticket.Flight.Route!.OriginAirport!.IATA,
                    b.Ticket.Flight.Route!.DestinationAirport!.IATA))
                .AsNoTracking()
                .ToListAsync();
        }

        // ---------- Extra “Tasks” items ----------


public async Task<List<OnTimePerfDto>> GetOnTimePerformanceAsync(
    DateTime fromUtc, DateTime toUtc, int toleranceMinutes, bool byRoute = true)
    {
        // 1) Project only simple scalars (EF can translate this fully)
        var rows = await _db.Flights
            .Where(f => f.DepartureUtc >= fromUtc && f.DepartureUtc <= toUtc)
            .Select(f => new
            {
                Key = byRoute
                    ? (f.Route!.OriginAirport!.IATA + "-" + f.Route!.DestinationAirport!.IATA)
                    : f.Aircraft!.TailNumber,   // or company, etc.
                                                // NOTE: you don’t have an actual vs scheduled arrival in the schema,
                                                // so we mark all flights as "on-time". When you add ActualArrivalUtc,
                                                // compute the bool here (within toleranceMinutes).
                OnTime = true
            })
            .AsNoTracking()
            .ToListAsync();

        // 2) Group & aggregate client-side (counts are small; avoids translation traps)
        var result = rows
            .GroupBy(x => x.Key)
            .Select(g =>
            {
                var flights = g.Count();
                var onTime = g.Count(x => x.OnTime);
                var late = flights - onTime;
                var pct = flights == 0 ? 0 : (int)Math.Round(100.0 * onTime / flights);
                return new OnTimePerfDto(g.Key, flights, onTime, late, pct);
            })
            .OrderByDescending(x => x.PctOnTime)
            .ToList();

        return result;
    }


    public async Task<List<CrewConflictDto>> GetCrewConflictsAsync(DateTime fromUtc, DateTime toUtc)
        {
            var legs = await _db.FlightCrews
                .Where(fc => fc.Flight!.DepartureUtc >= fromUtc && fc.Flight!.DepartureUtc <= toUtc)
                .Select(fc => new
                {
                    fc.CrewId,
                    fc.Crew!.FullName,
                    fc.FlightId,
                    fc.Flight!.DepartureUtc,
                    fc.Flight.ArrivalUtc
                })
                .AsNoTracking()
                .ToListAsync();

            var res = new List<CrewConflictDto>();
            foreach (var g in legs.GroupBy(x => new { x.CrewId, x.FullName }))
            {
                var arr = g.OrderBy(x => x.DepartureUtc).ToList();
                for (int i = 0; i < arr.Count; i++)
                    for (int j = i + 1; j < arr.Count; j++)
                    {
                        var a = arr[i]; var b = arr[j];
                        // overlap if a starts before b ends AND b starts before a ends
                        if (a.DepartureUtc < b.ArrivalUtc && b.DepartureUtc < a.ArrivalUtc)
                        {
                            res.Add(new CrewConflictDto(g.Key.CrewId, g.Key.FullName, a.FlightId, b.FlightId, a.DepartureUtc, b.DepartureUtc));
                        }
                    }
            }
            return res;
        }

        public async Task<List<PassengerConnectionDto>> GetPassengersWithConnectionsAsync(int maxLayoverHours)
        {
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

            var outList = new List<PassengerConnectionDto>();
            foreach (var g in legs.GroupBy(x => x.BookingId))
            {
                var ordered = g.OrderBy(x => x.DepartureUtc).ToList();
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var a = ordered[i];
                    var b = ordered[i + 1];
                    if (!string.Equals(a.DestIata, b.OriginIata, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var layover = (int)(b.DepartureUtc - a.ArrivalUtc).TotalMinutes;
                    if (layover < 0 || layover > maxLayoverHours * 60) continue;

                    outList.Add(new PassengerConnectionDto(
                        a.BookingRef, a.PassengerName,
                        a.FlightNumber, b.FlightNumber,
                        a.OriginIata, a.DestIata, b.DestIata,
                        a.DepartureUtc, a.ArrivalUtc, b.DepartureUtc, layover));
                }
            }
            return outList
                .OrderBy(r => r.PassengerName)
                .ThenBy(r => r.A_DepartureUtc)
                .ToList();
        }

        public async Task<List<FrequentFlierDto>> GetFrequentFliersAsync(int topN, bool byFlights)
        {
            var q = _db.Tickets
                .Select(t => new
                {
                    t.Booking!.Passenger!.FullName,
                    Dist = t.Flight!.Route!.DistanceKm
                });

            var grouped = await q
                .GroupBy(x => x.FullName)
                .Select(g => new FrequentFlierDto(
                    g.Key,
                    g.Count(),
                    g.Sum(x => x.Dist)))
                .ToListAsync();

            return (byFlights
                    ? grouped.OrderByDescending(x => x.Flights)
                    : grouped.OrderByDescending(x => x.DistanceKm))
                .Take(topN)
                .ToList();
        }

        public async Task<List<MaintenanceAlertDto>> GetMaintenanceAlertsAsync(int distanceThresholdKm, int olderThanDays)
        {
            var perAircraft = await _db.Flights
                .Select(f => new
                {
                    f.Aircraft!.TailNumber,
                    Dist = f.Route!.DistanceKm,
                    f.DepartureUtc
                })
                .GroupBy(x => x.TailNumber)
                .Select(g => new
                {
                    Tail = g.Key,
                    Flights = g.Count(),
                    DistKm = g.Sum(x => x.Dist)
                })
                .ToListAsync();

            var lastMaint = await _db.Maintenances
                .Where(m => m.CompletedUtc != null)
                .GroupBy(m => m.Aircraft!.TailNumber)
                .Select(g => new { Tail = g.Key, Last = g.Max(m => m.CompletedUtc) })
                .ToListAsync();

            var lastMap = lastMaint.ToDictionary(x => x.Tail, x => x.Last);

            var today = DateTime.UtcNow;
            return perAircraft
                .Select(a => new MaintenanceAlertDto(
                    a.Tail,
                    a.Flights,
                    a.DistKm,
                    lastMap.TryGetValue(a.Tail, out var d) ? d : null,
                    a.DistKm > distanceThresholdKm ||
                    (lastMap.TryGetValue(a.Tail, out var last) && (today - last!.Value).TotalDays > olderThanDays)))
                .OrderByDescending(x => x.NeedsAttention)
                .ThenByDescending(x => x.DistanceKm)
                .ToList();
        }

        public async Task<List<BaggageOverweightAlertDto>> GetBaggageOverweightAlertsAsync(decimal perTicketLimitKg)
        {
            var totals = await _db.Baggage
                .GroupBy(b => b.TicketId)
                .Select(g => new
                {
                    TicketId = g.Key,
                    Total = g.Sum(x => x.WeightKg),
                    BookingRef = g.Select(x => x.Ticket!.Booking!.BookingRef).First(),
                    Pax = g.Select(x => x.Ticket!.Booking!.Passenger!.FullName).First(),
                    FlightNo = g.Select(x => x.Ticket!.Flight!.FlightNumber).First()
                })
                .Where(x => x.Total > perTicketLimitKg)
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            return totals.Select(x =>
                new BaggageOverweightAlertDto(x.BookingRef, x.Pax, x.FlightNo, x.Total)).ToList();
        }

        public async Task<PagedFlightsDto> GetFlightsPageAsync(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc)
        {
            var q = _db.Flights.AsQueryable();
            if (fromUtc.HasValue) q = q.Where(f => f.DepartureUtc >= fromUtc.Value);
            if (toUtc.HasValue) q = q.Where(f => f.DepartureUtc <= toUtc.Value);

            var total = await q.CountAsync();

            var rows = await q
                .OrderBy(f => f.DepartureUtc)
                .Select(f => new FlightListRowDto(
                    f.FlightId,
                    f.FlightNumber,
                    f.Route!.OriginAirport!.IATA,
                    f.Route!.DestinationAirport!.IATA,
                    f.DepartureUtc))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return new PagedFlightsDto(page, pageSize, total, rows);
        }

        public async Task<Dictionary<string, int>> ToDictionaryFlightsByNumberAsync()
        {
            var pairs = await _db.Flights
                .GroupBy(f => f.FlightNumber)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            return pairs.ToDictionary(x => x.Key, x => x.Count);
        }

        public async Task<string[]> ToArrayTopRouteCodesAsync(int topN, DateTime fromUtc, DateTime toUtc)
        {
            return await _db.Tickets
                .Where(t => t.Flight!.DepartureUtc >= fromUtc && t.Flight.DepartureUtc <= toUtc)
                .GroupBy(t => t.Flight!.Route!.OriginAirport!.IATA + "-" + t.Flight!.Route!.DestinationAirport!.IATA)
                .Select(g => new { Code = g.Key, Rev = g.Sum(x => x.Fare) })
                .OrderByDescending(x => x.Rev)
                .Take(topN)
                .Select(x => x.Code)
                .ToArrayAsync();
        }


public async Task<List<DailyRevenueDto>> GetDailyRevenueRunningAsync(int daysBack)
    {
        // inclusive window from N-1 days ago thru today
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(daysBack - 1));

        // 1) Server-side: project only simple scalars (EF can translate)
        var rows = await _db.Tickets
            .Where(t => t.Flight!.DepartureUtc >= from)
            .Select(t => new
            {
                Day = t.Flight!.DepartureUtc.Date, // scalar date (translate ok)
                Fare = t.Fare
            })
            .AsNoTracking()
            .ToListAsync();

        // 2) Client-side: group by day and sum
        var perDay = rows
            .GroupBy(x => x.Day)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Fare));

        // 3) Fill every day in window + compute running total
        var result = new List<DailyRevenueDto>();
        decimal run = 0m;
        for (var d = from; d <= today; d = d.AddDays(1))
        {
            var rev = perDay.TryGetValue(d, out var v) ? v : 0m;
            run += rev;
            result.Add(new DailyRevenueDto(d, rev, run));
        }

        return result;
    }


    public async Task<List<ForecastDto>> GetNextWeekForecastAsync()
        {
            var start = DateTime.UtcNow.Date.AddDays(-14);

            var perDay = await _db.Tickets
                .Where(t => t.Flight!.DepartureUtc.Date >= start)
                .GroupBy(t => t.Flight!.DepartureUtc.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            var byDow = perDay
                .GroupBy(x => (int)x.Day.DayOfWeek)
                .ToDictionary(g => g.Key, g => g.Average(x => x.Count));

            var next = Enumerable.Range(1, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(i))
                .Select(d =>
                {
                    var dow = (int)d.DayOfWeek;
                    var avg = byDow.ContainsKey(dow) ? byDow[dow] : perDay.Average(x => x.Count);
                    return new ForecastDto(d, (int)Math.Round(avg));
                })
                .ToList();

            return next;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.SeedData
{
    /// <summary>
    /// Ad-hoc demo data packs to exercise Task menu items.
    /// Safe to run multiple times.
    /// </summary>
    public static class TestData
    {
        static readonly Random _rand = new Random(123);

        // ---- 0) Run everything with sensible defaults ----
        public static async Task RunAllAsync(FlightDbContext db)
        {
            await SalesBurstAsync(db, bookings: 200, maxSeatsPerBooking: 3);   // revenue/frequent fliers/forecast/daily revenue
            await ConnectionsAsync(db, count: 40, maxLayoverHours: 3);        // passengers with connections
            await OverweightBagsAsync(db, count: 60, minKg: 31m);             // baggage overweight alerts
            await DelaysAsync(db, count: 60, minDelayMin: 12, maxDelayMin: 45); // on-time performance (some “late”)
            await OldMaintenanceAsync(db, count: 6, daysAgo: 45);             // maintenance alert (older than threshold)
        }

        // ---- 1) Add extra bookings/tickets to many flights (revenue, frequent fliers, forecast…) ----
        public static async Task SalesBurstAsync(FlightDbContext db, int bookings, int maxSeatsPerBooking = 3)
        {
            var pax = await db.Passengers.AsNoTracking().ToListAsync();
            var flights = await db.Flights.Include(f => f.Aircraft).AsNoTracking()
                             .Where(f => f.DepartureUtc >= DateTime.UtcNow.AddDays(-20) &&
                                         f.DepartureUtc <= DateTime.UtcNow.AddDays(20))
                             .ToListAsync();
            if (pax.Count == 0 || flights.Count == 0) return;

            var newBookings = new List<Booking>();
            var newTickets = new List<Ticket>();

            // seed seat maps (used seats) per flight to avoid duplicates in this run
            var usedSeats = await db.Tickets
                .GroupBy(t => t.FlightId)
                .Select(g => new { FlightId = g.Key, Seats = g.Select(t => t.SeatNumber) })
                .ToListAsync();
            var seatMap = flights.ToDictionary(
                f => f.FlightId,
                f => new HashSet<string>(
                        usedSeats.FirstOrDefault(x => x.FlightId == f.FlightId)?.Seats ?? Enumerable.Empty<string>(),
                        StringComparer.OrdinalIgnoreCase));

            for (int n = 0; n < bookings; n++)
            {
                var p = pax[_rand.Next(pax.Count)];
                var fl = flights[_rand.Next(flights.Count)];
                var seatsWanted = 1 + _rand.Next(Math.Max(1, maxSeatsPerBooking));
                var cap = Math.Max(10, fl.Aircraft?.Capacity ?? 180);
                var taken = seatMap[fl.FlightId];

                var bk = new Booking
                {
                    PassengerId = p.PassengerId,
                    BookingDate = DateTime.UtcNow.AddDays(-_rand.Next(25)),
                    BookingRef = $"B{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status = BookingStatus.Confirmed
                };
                newBookings.Add(bk);

                for (int i = 0; i < seatsWanted; i++)
                {
                    var guard = 0;
                    string seat;
                    do
                    {
                        seat = $"S{_rand.Next(1, cap + 1):000}";
                        if (++guard > cap * 2) break;
                    } while (!taken.Add(seat));

                    newTickets.Add(new Ticket
                    {
                        Booking = bk,
                        FlightId = fl.FlightId,
                        SeatNumber = seat,
                        Fare = 40m + (decimal)_rand.NextDouble() * 160m,
                        CheckedIn = false
                    });
                }
            }

            db.Bookings.AddRange(newBookings);
            db.Tickets.AddRange(newTickets);
            await db.SaveChangesAsync();
        }

        // ---- 2) Create bookings with 2 legs (A→B then B→C within X hours) ----
        public static async Task ConnectionsAsync(FlightDbContext db, int count, int maxLayoverHours)
        {
            var pax = await db.Passengers.AsNoTracking().ToListAsync();
            // Grab flights with their route+airports for matching
            var legs = await db.Flights
                .Include(f => f.Route)!.ThenInclude(r => r.OriginAirport)
                .Include(f => f.Route)!.ThenInclude(r => r.DestinationAirport)
                .Where(f => f.DepartureUtc >= DateTime.UtcNow.AddDays(-10) &&
                            f.DepartureUtc <= DateTime.UtcNow.AddDays(20))
                .AsNoTracking()
                .ToListAsync();
            if (pax.Count == 0 || legs.Count == 0) return;

            // index by Origin IATA to quickly find next legs
            var byOrigin = legs.GroupBy(f => f.Route!.OriginAirport!.IATA)
                               .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartureUtc).ToList());

            var newBookings = new List<Booking>();
            var newTickets = new List<Ticket>();

            for (int i = 0; i < count; i++)
            {
                var first = legs[_rand.Next(legs.Count)];
                var hub = first.Route!.DestinationAirport!.IATA;

                if (!byOrigin.TryGetValue(hub, out var candidates)) continue;

                // find a second leg that leaves from hub shortly after first arrives
                var after = first.ArrivalUtc;
                var second = candidates.FirstOrDefault(x =>
                    x.DepartureUtc >= after &&
                    x.DepartureUtc <= after.AddHours(maxLayoverHours));

                if (second == null) continue;

                var p = pax[_rand.Next(pax.Count)];
                var bk = new Booking
                {
                    PassengerId = p.PassengerId,
                    BookingDate = DateTime.UtcNow,
                    BookingRef = $"C{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status = BookingStatus.Confirmed
                };
                newBookings.Add(bk);

                newTickets.Add(new Ticket { Booking = bk, FlightId = first.FlightId, SeatNumber = "S999", Fare = 60m });
                newTickets.Add(new Ticket { Booking = bk, FlightId = second.FlightId, SeatNumber = "S998", Fare = 60m });
            }

            db.Bookings.AddRange(newBookings);
            db.Tickets.AddRange(newTickets);
            await db.SaveChangesAsync();
        }

        // ---- 3) Add overweight bags to random tickets ----
        public static async Task OverweightBagsAsync(FlightDbContext db, int count, decimal minKg = 31m)
        {
            var tickets = await db.Tickets.Include(t => t.Baggage).AsNoTracking().ToListAsync();
            if (tickets.Count == 0) return;

            var bags = new List<Baggage>();
            foreach (var t in tickets.OrderBy(_ => _rand.Next()).Take(count))
            {
                var pieces = 1 + _rand.Next(2); // 1..2 pieces
                decimal total = 0m;
                for (int i = 0; i < pieces; i++)
                {
                    var w = Math.Round(12m + (decimal)_rand.NextDouble() * 15m, 2);
                    total += w;
                    bags.Add(new Baggage
                    {
                        TicketId = t.TicketId,
                        TagNumber = $"BG{_rand.Next(100000, 999999)}",
                        WeightKg = w
                    });
                }
                // if still not above threshold, push it over
                if (total < minKg)
                {
                    bags.Add(new Baggage
                    {
                        TicketId = t.TicketId,
                        TagNumber = $"BG{_rand.Next(100000, 999999)}",
                        WeightKg = Math.Round(minKg - total + 1m, 2)
                    });
                }
            }
            db.Baggage.AddRange(bags);
            await db.SaveChangesAsync();
        }

        // ---- 4) Mark random flights as delayed (affects on-time performance) ----
     public static async Task DelaysAsync(FlightDbContext db, int count, int minDelayMin, int maxDelayMin)
{
    var flights = await db.Flights
        .Where(f => f.DepartureUtc >= DateTime.UtcNow.AddDays(-10) &&
                    f.DepartureUtc <= DateTime.UtcNow.AddDays(10))
        .OrderBy(_ => Guid.NewGuid())
        .Take(count)
        .ToListAsync();

    foreach (var f in flights)
    {
        var delay = TimeSpan.FromMinutes(_rand.Next(minDelayMin, maxDelayMin + 1));
        f.ArrivalUtc = f.ArrivalUtc.Add(delay);

                // optional: reflect the delay in status
                if (f.Status == FlightStatus.Scheduled) ;
              // <-- not "Delay"
    }
    await db.SaveChangesAsync();
}


        // ---- 5) Add old maintenance records to trigger alerts ----
        public static async Task OldMaintenanceAsync(FlightDbContext db, int count = 5, int daysAgo = 45)
        {
            var fleet = await db.Aircraft.AsNoTracking().ToListAsync();
            if (fleet.Count == 0) return;

            var list = new List<AircraftMaintenance>();
            for (int i = 0; i < count; i++)
            {
                var ac = fleet[_rand.Next(fleet.Count)];
                var when = DateTime.UtcNow.AddDays(-daysAgo - _rand.Next(10));
                list.Add(new AircraftMaintenance
                {
                    AircraftId = ac.AircraftId,
                    WorkType = i % 2 == 0 ? "A-check" : "Inspection",
                    Notes = "Demo old maintenance",
                    ScheduledUtc = when,
                    CompletedUtc = i % 3 == 0 ? null : when.AddHours(2),
                    GroundsAircraft = i % 4 == 0
                });
            }
            db.Maintenances.AddRange(list);
            await db.SaveChangesAsync();
        }
    }
}

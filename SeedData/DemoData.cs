using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlightApp.Data;
using FlightApp.Models;

namespace FlightApp.SeedData
{
    public static class DemoData
    {
        private static readonly Random _rand = new Random();

        // ------------------------------------------------------------
        // 0) Simulate delays WITHOUT adding a new enum value
        // ------------------------------------------------------------
        public static async Task DelaysAsync(FlightDbContext db, int count, int minDelayMin, int maxDelayMin)
        {
            var now = DateTime.UtcNow;

            var flights = await db.Flights
                .Where(f =>
                    f.Status != FlightStatus.Canceled &&
                    f.DepartureUtc >= now.AddDays(-10) &&
                    f.DepartureUtc <= now.AddDays(10))
                .OrderBy(_ => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            foreach (var f in flights)
            {
                var delay = TimeSpan.FromMinutes(_rand.Next(minDelayMin, maxDelayMin + 1));

                // simulate a late arrival
                f.ArrivalUtc = f.ArrivalUtc.Add(delay);

                // keep status coherent using current enum values
                if (f.Status == FlightStatus.Scheduled && f.DepartureUtc <= now)
                    f.Status = FlightStatus.Departed;

                if (f.Status == FlightStatus.Departed && f.ArrivalUtc <= now)
                    f.Status = FlightStatus.Landed;
            }

            await db.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // 1) Make several flights hit >= minPercent occupancy (for Task #4)
        // ------------------------------------------------------------
        public static async Task MakeHighOccupancyAsync(
            FlightDbContext db, int minPercent = 85, int flightsToBoost = 6)
        {
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow.AddDays(7);

            var candidates = await db.Flights
                .Include(f => f.Aircraft)
                .Where(f => f.DepartureUtc >= from && f.DepartureUtc <= to)
                .OrderBy(_ => Guid.NewGuid())
                .Take(flightsToBoost * 3) // over-pick in case some are already high
                .ToListAsync();

            int boosted = 0;

            foreach (var f in candidates)
            {
                var cap = f.Aircraft?.Capacity ?? 180;
                if (cap <= 0) continue;

                var sold = await db.Tickets.CountAsync(t => t.FlightId == f.FlightId);
                var target = (int)Math.Ceiling(cap * (minPercent / 100.0));

                if (sold >= target) continue; // already above threshold

                var toAdd = Math.Min(target - sold, Math.Max(1, cap / 5)); // add up to ~20%

                var pax = await AnyPassengerAsync(db);
                var booking = new Booking
                {
                    PassengerId = pax.PassengerId,
                    BookingRef = $"D{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
                    Status = BookingStatus.Confirmed,
                    BookingDate = DateTime.UtcNow
                };
                db.Bookings.Add(booking);

                var freeSeats = NextFreeSeats(db, f.FlightId, cap, toAdd).ToList();

                foreach (var seat in freeSeats)
                {
                    db.Tickets.Add(new Ticket
                    {
                        Booking = booking,
                        FlightId = f.FlightId,
                        SeatNumber = seat,
                        Fare = 80m + (decimal)_rand.NextDouble() * 120m,
                        CheckedIn = false
                    });
                }

                await db.SaveChangesAsync();
                if (++boosted >= flightsToBoost) break;
            }
        }

        // ------------------------------------------------------------
        // 2) Create true A→B connection itineraries (for Task #7)
        //    Same booking, A.Dest == B.Origin, layover <= X hours
        // ------------------------------------------------------------
        public static async Task MakeConnectionItinerariesAsync(
            FlightDbContext db, int layoverHours = 3, int itineraries = 8)
        {
            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow.AddDays(7);
            var maxLay = TimeSpan.FromHours(layoverHours);

            var flights = await db.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Route)!.ThenInclude(r => r!.OriginAirport)
                .Include(f => f.Route)!.ThenInclude(r => r!.DestinationAirport)
                .Where(f => f.DepartureUtc >= from && f.DepartureUtc <= to)
                .OrderBy(f => f.DepartureUtc)
                .ToListAsync();

            // Build quick lookup by OriginAirportId for the second leg
            var byOrigin = flights
                .GroupBy(f => f.Route!.OriginAirport!.AirportId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int made = 0;

            foreach (var a in flights)
            {
                var destId = a.Route!.DestinationAirport!.AirportId;
                if (!byOrigin.TryGetValue(destId, out var candidates)) continue;

                // Find a second leg that departs soon after A arrives
                var b = candidates
                    .FirstOrDefault(x => x.DepartureUtc >= a.ArrivalUtc &&
                                         (x.DepartureUtc - a.ArrivalUtc) <= maxLay);
                if (b == null) continue;

                var pax = await AnyPassengerAsync(db);
                var booking = new Booking
                {
                    PassengerId = pax.PassengerId,
                    BookingRef = $"C{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
                    Status = BookingStatus.Confirmed,
                    BookingDate = DateTime.UtcNow
                };
                db.Bookings.Add(booking);

                // Seat on A
                var seatA = NextFreeSeats(db, a.FlightId, a.Aircraft?.Capacity ?? 180, 1).FirstOrDefault() ?? "S001";
                db.Tickets.Add(new Ticket
                {
                    Booking = booking,
                    FlightId = a.FlightId,
                    SeatNumber = seatA,
                    Fare = 100m + (decimal)_rand.NextDouble() * 120m,
                    CheckedIn = false
                });

                // Seat on B
                var seatB = NextFreeSeats(db, b.FlightId, b.Aircraft?.Capacity ?? 180, 1).FirstOrDefault() ?? "S001";
                db.Tickets.Add(new Ticket
                {
                    Booking = booking,
                    FlightId = b.FlightId,
                    SeatNumber = seatB,
                    Fare = 100m + (decimal)_rand.NextDouble() * 120m,
                    CheckedIn = false
                });

                await db.SaveChangesAsync();
                if (++made >= itineraries) break;
            }
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------
        private static async Task<Passenger> AnyPassengerAsync(FlightDbContext db)
        {
            var p = await db.Passengers.OrderBy(_ => Guid.NewGuid()).FirstOrDefaultAsync();
            if (p != null) return p;

            p = new Passenger
            {
                FullName = "Demo Passenger",
                PassportNo = "PD" + _rand.Next(1000000, 9999999),
                Nationality = "OM",
                DOB = new DateTime(1988, 5, 10)
            };
            db.Passengers.Add(p);
            await db.SaveChangesAsync();
            return p;
        }

        private static IEnumerable<string> NextFreeSeats(FlightDbContext db, int flightId, int capacity, int needed)
        {
            var taken = new HashSet<string>(
                db.Tickets.Where(t => t.FlightId == flightId).Select(t => t.SeatNumber),
                StringComparer.OrdinalIgnoreCase);

            int added = 0;
            for (int i = 1; i <= Math.Max(1, capacity); i++)
            {
                var code = $"S{i:000}";
                if (taken.Add(code))
                {
                    yield return code;
                    if (++added >= needed) yield break;
                }
            }
        }
        public static async Task SeedHighOccupancyAsync(
           FlightDbContext db,
           int minPercent = 85,
           int daysWindow = 7,
           int howManyFlights = 6)
        {
            var now = DateTime.UtcNow;
            var from = now.AddDays(-daysWindow);
            var to = now.AddDays(daysWindow);

            // Candidates: flights with capacity > 0 in window
            var flights = await db.Flights
                .Include(f => f.Aircraft)
                .Include(f => f.Tickets)
                .Where(f => f.DepartureUtc >= from &&
                            f.DepartureUtc <= to &&
                            f.Aircraft != null &&
                            f.Aircraft.Capacity > 0)
                .OrderBy(_ => Guid.NewGuid())
                .Take(howManyFlights)
                .ToListAsync();

            if (!flights.Any()) return;

            // Reuse existing passengers; create a few new if we run out.
            var paxPool = await db.Passengers.OrderBy(_ => Guid.NewGuid()).Take(50).ToListAsync();
            if (paxPool.Count < flights.Count)
            {
                int needed = flights.Count - paxPool.Count;
                var fresh = Enumerable.Range(1, needed).Select(i => new Passenger
                {
                    FullName = $"Demo Pax {Guid.NewGuid():N}".Substring(0, 16),
                    PassportNo = $"DP{_rand.Next(1000000, 9999999)}",
                    Nationality = "OM",
                    DOB = new DateTime(1985, 1, 1).AddDays(_rand.Next(10000))
                }).ToList();
                db.Passengers.AddRange(fresh);
                await db.SaveChangesAsync();
                paxPool.AddRange(fresh);
            }

            foreach (var f in flights)
            {
                int cap = f.Aircraft!.Capacity;
                int sold = f.Tickets.Count;
                int target = (int)Math.Ceiling(cap * (minPercent / 100.0));

                int need = Math.Min(Math.Max(target - sold, 0), cap - sold);
                if (need <= 0) continue;

                // Build a set of taken seats like S001..S220
                var taken = new HashSet<string>(
                    f.Tickets.Where(t => t.SeatNumber != null)
                             .Select(t => t.SeatNumber!),
                    StringComparer.OrdinalIgnoreCase);

                // Simple seat generator
                IEnumerable<string> NextSeats(int n)
                {
                    int i = 1;
                    while (n > 0 && i <= cap)
                    {
                        var s = $"S{i:000}";
                        if (taken.Add(s)) { yield return s; n--; }
                        i++;
                    }
                }

                // Use one booking per flight (easier), attach N tickets
                var pax = paxPool[_rand.Next(paxPool.Count)];
                var booking = new Booking
                {
                    PassengerId = pax.PassengerId,
                    BookingRef = $"B{Guid.NewGuid():N}".Substring(0, 8).ToUpperInvariant(),
                    Status = BookingStatus.Confirmed,
                    BookingDate = now
                };
                db.Bookings.Add(booking);

                foreach (var seat in NextSeats(need))
                {
                    db.Tickets.Add(new Ticket
                    {
                        Booking = booking,
                        FlightId = f.FlightId,
                        SeatNumber = seat,
                        Fare = Math.Round(50m + (decimal)_rand.NextDouble() * 150m, 2),
                        CheckedIn = false
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}

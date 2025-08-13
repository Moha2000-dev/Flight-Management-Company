using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.SeedData
{
    public static class SeedData
    {
        public static async Task EnsureSeededAsync(FlightDbContext db)
        {
            // Already seeded?
            if (await db.Airports.AnyAsync()) return;

            var rand = new Random(7);

            // ---------- Airports ----------
            var airports = new[]
            {
                new Airport{ IATA="MCT", Name="Muscat Intl",   City="Muscat",   Country="Oman"    },
                new Airport{ IATA="DXB", Name="Dubai Intl",    City="Dubai",    Country="UAE"     },
                new Airport{ IATA="DOH", Name="Hamad Intl",    City="Doha",     Country="Qatar"   },
                new Airport{ IATA="AUH", Name="Abu Dhabi",     City="AbuDhabi", Country="UAE"     },
                new Airport{ IATA="JED", Name="King Abdulaziz",City="Jeddah",   Country="KSA"     },
                new Airport{ IATA="RUH", Name="King Khalid",   City="Riyadh",   Country="KSA"     },
                new Airport{ IATA="BAH", Name="Bahrain Intl",  City="Manama",   Country="Bahrain" },
                new Airport{ IATA="KWI", Name="Kuwait Intl",   City="Kuwait",   Country="Kuwait"  },
                new Airport{ IATA="CAI", Name="Cairo Intl",    City="Cairo",    Country="Egypt"   },
                new Airport{ IATA="AMM", Name="Queen Alia",    City="Amman",    Country="Jordan"  }
            };
            db.Airports.AddRange(airports);
            await db.SaveChangesAsync();

            Airport A(string iata) => airports.First(x => x.IATA == iata);

            // ---------- Routes ----------
            var routePairs = new (string o, string d, int km)[]
            {
                ("MCT","DXB",347), ("DXB","MCT",347),
                ("DXB","DOH",379), ("DOH","DXB",379),
                ("DXB","RUH",869), ("RUH","DXB",869),
                ("BAH","DXB",484), ("DXB","BAH",484),
                ("MCT","DOH",704), ("DOH","MCT",704),
                ("MCT","AUH",377), ("AUH","MCT",377),
                ("JED","RUH",853), ("RUH","JED",853),
                ("DXB","AMM",2020),("AMM","DXB",2020),
                ("CAI","DXB",2420),("DXB","CAI",2420),
                ("KWI","DXB",851), ("DXB","KWI",851)
            };

            var routes = routePairs.Select(x => new Route
            {
                OriginAirport = A(x.o),
                DestinationAirport = A(x.d),
                DistanceKm = x.km
            }).ToList();

            db.Routes.AddRange(routes);
            await db.SaveChangesAsync();

            // ---------- Aircraft ----------
            var fleet = Enumerable.Range(1, 10).Select(i => new Aircraft
            {
                TailNumber = $"A9C-{100 + i}",
                Model = i % 3 == 0 ? "A321" : (i % 2 == 0 ? "A320" : "B737-800"),
                Capacity = i % 3 == 0 ? 220 : (i % 2 == 0 ? 180 : 186)
            }).ToList();

            db.Aircraft.AddRange(fleet);
            await db.SaveChangesAsync();

            // ---------- Crew ----------
            var roleCycle = new[] { CrewRole.Pilot, CrewRole.CoPilot, CrewRole.FlightAttendant, CrewRole.FlightAttendant };

            var crew = Enumerable.Range(1, 60).Select(i => new CrewMember
            {
                FullName = $"Crew {i:000}",
                Role = roleCycle[i % roleCycle.Length],
                LicenseNo = $"LIC-{i:0000}"
            }).ToList();

            db.CrewMembers.AddRange(crew);
            await db.SaveChangesAsync();

            // ---------- Passengers ----------
            var nationalities = new[] { "OM", "AE", "QA", "SA", "BH", "KW", "EG", "JO" };

            var pax = Enumerable.Range(1, 120).Select(i => new Passenger
            {
                FullName = $"Passenger {i:000}",
                PassportNo = $"P{i:0000000}",
                Nationality = nationalities[rand.Next(nationalities.Length)],
                DOB = new DateTime(1970, 1, 1).AddDays(rand.Next(18_000))
            }).ToList();

            db.Passengers.AddRange(pax);
            await db.SaveChangesAsync();

            // ---------- Flights (next 30 days ~6/day) ----------
            var flights = new List<Flight>();
            var utc0 = DateTime.UtcNow.Date; // today 00:00 UTC

            for (int day = 0; day < 30; day++)
            {
                var dayStart = utc0.AddDays(day);
                // pick 6 random routes per day
                foreach (var r in routes.OrderBy(_ => rand.Next()).Take(6))
                {
                    var ac = fleet[rand.Next(fleet.Count)];
                    var dep = dayStart.AddHours(6 + rand.Next(16)); // between 06:00–22:00
                    var dur = TimeSpan.FromMinutes(60 + rand.Next(120));

                    flights.Add(new Flight
                    {
                        FlightNumber = $"FM{100 + rand.Next(400)}",
                        Route = r,
                        Aircraft = ac,
                        DepartureUtc = dep,
                        ArrivalUtc = dep + dur,
                        Status = FlightStatus.Scheduled
                    });
                }
            }

            db.Flights.AddRange(flights);
            await db.SaveChangesAsync(); // assigns FlightId

            // ---------- Flight Crew assignments ----------
            var flightCrews = new List<FlightCrew>();
            var pilots = crew.Where(c => c.Role == CrewRole.Pilot).ToList();
            var fos = crew.Where(c => c.Role == CrewRole.CoPilot).ToList();
            var fas = crew.Where(c => c.Role == CrewRole.FlightAttendant).ToList();

            foreach (var f in flights)
            {
                var cpt = pilots.OrderBy(_ => rand.Next()).First();
                var fo = fos.OrderBy(_ => rand.Next()).First();
                var fa1 = fas.OrderBy(_ => rand.Next()).First();
                var fa2 = fas.OrderBy(_ => rand.Next()).Skip(1).FirstOrDefault() ?? fa1;

                flightCrews.Add(new FlightCrew { Flight = f, Crew = cpt, RoleOnFlight = "Captain" });
                flightCrews.Add(new FlightCrew { Flight = f, Crew = fo, RoleOnFlight = "First Officer" });
                flightCrews.Add(new FlightCrew { Flight = f, Crew = fa1, RoleOnFlight = "FA" });
                flightCrews.Add(new FlightCrew { Flight = f, Crew = fa2, RoleOnFlight = "FA" });
            }

            db.FlightCrews.AddRange(flightCrews);
            await db.SaveChangesAsync();

            // ---------- Seat map per flight (prevents duplicate seats during this run) ----------
            var seatMap = flights.ToDictionary(
                f => f.FlightId,
                f => new HashSet<string>(
                    db.Tickets.Where(t => t.FlightId == f.FlightId).Select(t => t.SeatNumber),
                    StringComparer.OrdinalIgnoreCase));

            // ---------- Bookings & Tickets ----------
            var bookings = new List<Booking>();
            var tickets = new List<Ticket>();

            foreach (var n in Enumerable.Range(1, 200))
            {
                var p = pax[rand.Next(pax.Count)];
                var fl = flights[rand.Next(flights.Count)];
                var seatsWanted = 1 + rand.Next(3); // 1..3

                var booking = new Booking
                {
                    PassengerId = p.PassengerId,
                    BookingRef = $"B{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status = BookingStatus.Confirmed,
                    BookingDate = utc0.AddDays(rand.Next(30))
                };

                var taken = seatMap[fl.FlightId];
                var cap = Math.Max(10, fl.Aircraft?.Capacity ?? 180);

                for (int i = 0; i < seatsWanted; i++)
                {
                    string seat;
                    int guard = 0;
                    do
                    {
                        // upper bound is exclusive → use cap + 1
                        seat = $"S{rand.Next(1, cap + 1):000}";
                        if (++guard > cap * 2) break; // break if flight almost full
                    }
                    while (!taken.Add(seat));

                    tickets.Add(new Ticket
                    {
                        Booking = booking,
                        Flight = fl,
                        SeatNumber = seat,
                        Fare = 25m + (decimal)rand.NextDouble() * 120m,
                        CheckedIn = false
                    });
                }

                bookings.Add(booking);
            }

            db.Bookings.AddRange(bookings);
            db.Tickets.AddRange(tickets);
            await db.SaveChangesAsync();

            // ---------- Baggage ----------
            var bags = tickets
                .OrderBy(_ => rand.Next())
                .Take(150)
                .Select(t => new Baggage
                {
                    Ticket = t,
                    TagNumber = $"BG{rand.Next(100000, 999999)}",
                    WeightKg = Math.Round(12m + (decimal)rand.NextDouble() * 18m, 2)
                })
                .ToList();

            db.Baggage.AddRange(bags);

            // ---------- Maintenance (half open, half done) ----------
            var maint = Enumerable.Range(1, 20).Select(i =>
            {
                var ac = fleet[rand.Next(fleet.Count)];
                var sch = utc0.AddDays(rand.Next(30)).AddHours(rand.Next(24));
                var done = i % 2 == 0 ? (DateTime?)null : sch.AddHours(2);

                return new AircraftMaintenance
                {
                    AircraftId = ac.AircraftId,
                    WorkType = i % 3 == 0 ? "A-check" : "Inspection",
                    Notes = "Auto seeded",
                    ScheduledUtc = sch,
                    CompletedUtc = done,
                    GroundsAircraft = i % 5 == 0
                };
            }).ToList();

            db.Maintenances.AddRange(maint);

            await db.SaveChangesAsync();
        }
    }
}

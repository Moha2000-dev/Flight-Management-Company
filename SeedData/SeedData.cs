using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.SeedData
{
    public static class SeedData
    {
        public static async Task EnsureSeededAsync(FlightDbContext db)
        {
            if (await db.Airports.AnyAsync()) return;

            var rand = new Random(7);

            // --- Airports (10) ---
            var airports = new[]
            {
                new Airport{ IATA="MCT", Name="Muscat Intl",   City="Muscat",  Country="Oman" },
                new Airport{ IATA="DXB", Name="Dubai Intl",    City="Dubai",   Country="UAE"  },
                new Airport{ IATA="DOH", Name="Hamad Intl",    City="Doha",    Country="Qatar"},
                new Airport{ IATA="AUH", Name="Abu Dhabi",     City="AbuDhabi",Country="UAE"  },
                new Airport{ IATA="JED", Name="King Abdulaziz",City="Jeddah",  Country="KSA"  },
                new Airport{ IATA="RUH", Name="King Khalid",   City="Riyadh",  Country="KSA"  },
                new Airport{ IATA="BAH", Name="Bahrain Intl",  City="Manama",  Country="Bahrain"},
                new Airport{ IATA="KWI", Name="Kuwait Intl",   City="Kuwait",  Country="Kuwait"},
                new Airport{ IATA="CAI", Name="Cairo Intl",    City="Cairo",   Country="Egypt"},
                new Airport{ IATA="AMM", Name="Queen Alia",    City="Amman",   Country="Jordan"}
            };
            db.Airports.AddRange(airports);
            await db.SaveChangesAsync();

            Airport A(string i) => airports.First(x => x.IATA == i);

            // --- Routes (20) ---
            var routePairs = new (string o, string d, int km)[]
            {
                ("MCT","DXB",347),("DXB","MCT",347),("DXB","DOH",379),("DOH","DXB",379),
                ("DXB","RUH",869),("RUH","DXB",869),("BAH","DXB",484),("DXB","BAH",484),
                ("MCT","DOH",704),("DOH","MCT",704),("MCT","AUH",377),("AUH","MCT",377),
                ("JED","RUH",853),("RUH","JED",853),("DXB","AMM",2020),("AMM","DXB",2020),
                ("CAI","DXB",2420),("DXB","CAI",2420),("KWI","DXB",851),("DXB","KWI",851)
            };
            var routes = routePairs.Select(x => new Route
            {
                OriginAirport = A(x.o),
                DestinationAirport = A(x.d),
                DistanceKm = x.km
            }).ToList();
            db.Routes.AddRange(routes);
            await db.SaveChangesAsync();

            // --- Aircraft (10) ---
            var fleet = Enumerable.Range(1, 10).Select(i => new Aircraft
            {
                TailNumber = $"A9C-{100 + i}",
                Model = i % 3 == 0 ? "A321" : (i % 2 == 0 ? "A320" : "B737-800"),
                Capacity = i % 3 == 0 ? 220 : (i % 2 == 0 ? 180 : 186)
            }).ToList();
            db.Aircraft.AddRange(fleet);
            await db.SaveChangesAsync();

            // --- Crew (60) ---
            var crew = Enumerable.Range(1, 60).Select(i => new CrewMember
            {
                FullName = $"Crew {i:000}",
                Role = (CrewRole)(i % 4), // Pilot/CoPilot/Cabin/Safety
                LicenseNo = $"LIC-{i:0000}"
            }).ToList();
            db.CrewMembers.AddRange(crew);

            // --- Passengers (120) ---
            var pax = Enumerable.Range(1, 120).Select(i => new Passenger
            {
                FullName = $"Passenger {i:000}",
                PassportNo = $"P{i:0000000}",
                Nationality = new[] { "OM", "AE", "QA", "SA", "BH", "KW", "EG", "JO" }[rand.Next(8)],
                DOB = new DateTime(1970, 1, 1).AddDays(rand.Next(18000))
            }).ToList();
            db.Passengers.AddRange(pax);
            await db.SaveChangesAsync();

            // --- Flights (30 days × ~6/day = ~180) ---
            DateTime utc0 = DateTime.UtcNow.Date;
            var flights = new List<Flight>();
            for (int day = 0; day < 30; day++)
            {
                var dayStart = utc0.AddDays(day);
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
            await db.SaveChangesAsync();

            // --- Bookings/Tickets (200/400) ---
            var bookings = new List<Booking>();
            var tickets = new List<Ticket>();
            foreach (var n in Enumerable.Range(1, 200))
            {
                var p = pax[rand.Next(pax.Count)];
                var fl = flights[rand.Next(flights.Count)];
                var seats = 1 + rand.Next(3); // 1..3
                var booking = new Booking
                {
                    PassengerId = p.PassengerId,
                    BookingRef = $"B{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status = BookingStatus.Confirmed,
                    BookingDate = utc0.AddDays(rand.Next(30))
                };
                // chosen unique seats for the flight
                var taken = new HashSet<string>(
                    db.Tickets.Where(t => t.FlightId == fl.FlightId).Select(t => t.SeatNumber));
                var seatList = new List<string>();
                for (int i = 0; i < seats; i++)
                {
                    string seat;
                    do { seat = $"S{rand.Next(1, Math.Max(10, fl.Aircraft!.Capacity)):000}"; }
                    while (!taken.Add(seat));
                    seatList.Add(seat);
                }
                foreach (var s in seatList)
                    tickets.Add(new Ticket { Booking = booking, Flight = fl, SeatNumber = s, Fare = 25 + (decimal)rand.NextDouble() * 120m });

                bookings.Add(booking);
            }
            db.Bookings.AddRange(bookings);
            db.Tickets.AddRange(tickets);
            await db.SaveChangesAsync();

            // --- Baggage (150) ---
            var bags = new List<Baggage>();
            foreach (var t in tickets.OrderBy(_ => rand.Next()).Take(150))
                bags.Add(new Baggage { Ticket = t, TagNumber = $"BG{rand.Next(100000, 999999)}", WeightKg = Math.Round((decimal)(12 + rand.NextDouble() * 18), 2) });
            db.Baggage.AddRange(bags);

            // --- Maintenance (20; half open) ---
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

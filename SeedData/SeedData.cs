using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.SeedData
{
    public static class SeedData
    {
        public static async Task EnsureSeededAsync(FlightDbContext db)
        {
            // already seeded?
            if (await db.Airports.AnyAsync()) return;

            // --- Airports ---
            var ap = new[]
            {
                new Airport { IATA = "MCT", Name = "Muscat Intl", City = "Muscat", Country = "Oman" },
                new Airport { IATA = "DXB", Name = "Dubai Intl",  City = "Dubai",  Country = "UAE"  },
                new Airport { IATA = "DOH", Name = "Hamad Intl",  City = "Doha",  Country = "Qatar" },
                new Airport { IATA = "RUH", Name = "King Khalid", City = "Riyadh",Country = "Saudi Arabia" },
                new Airport { IATA = "JED", Name = "King Abdulaziz", City = "Jeddah", Country = "Saudi Arabia" },
                new Airport { IATA = "BAH", Name = "Bahrain Intl", City = "Manama", Country = "Bahrain" }
            };
            db.Airports.AddRange(ap);
            await db.SaveChangesAsync();

            // helper
            Airport A(string iata) => ap.First(a => a.IATA == iata);

            // --- Routes ---
            var routes = new[]
            {
                new Route { OriginAirport = A("MCT"), DestinationAirport = A("DXB"), DistanceKm = 347 },
                new Route { OriginAirport = A("MCT"), DestinationAirport = A("DOH"), DistanceKm = 704 },
                new Route { OriginAirport = A("DXB"), DestinationAirport = A("DOH"), DistanceKm = 379 },
                new Route { OriginAirport = A("DXB"), DestinationAirport = A("RUH"), DistanceKm = 869 },
                new Route { OriginAirport = A("BAH"), DestinationAirport = A("DXB"), DistanceKm = 484 },
            };
            db.Routes.AddRange(routes);
            await db.SaveChangesAsync();

            // --- Aircraft ---
            var fleet = new[]
            {
                new Aircraft { TailNumber = "A9C-001", Model = "A320", Capacity = 180 },
                new Aircraft { TailNumber = "A9C-002", Model = "A320", Capacity = 180 },
                new Aircraft { TailNumber = "A9C-737", Model = "B737-800", Capacity = 186 }
            };
            db.Aircraft.AddRange(fleet);
            await db.SaveChangesAsync();

            // --- Flights (next 3 days) ---
            DateTime D(int day, int hourUtc) => DateTime.UtcNow.Date.AddDays(day).AddHours(hourUtc);

            var f1 = new Flight
            {
                FlightNumber = "FM101",
                Route = routes.First(r => r.OriginAirport!.IATA == "MCT" && r.DestinationAirport!.IATA == "DXB"),
                Aircraft = fleet[0],
                DepartureUtc = D(1, 8),
                ArrivalUtc = D(1, 9),
                Status = FlightStatus.Scheduled
            };
            var f2 = new Flight
            {
                FlightNumber = "FM102",
                Route = routes.First(r => r.OriginAirport!.IATA == "DXB" && r.DestinationAirport!.IATA == "MCT"),
                Aircraft = fleet[1],
                DepartureUtc = D(1, 18),
                ArrivalUtc = D(1, 19),
                Status = FlightStatus.Scheduled
            };
            var f3 = new Flight
            {
                FlightNumber = "FM201",
                Route = routes.First(r => r.OriginAirport!.IATA == "DXB" && r.DestinationAirport!.IATA == "DOH"),
                Aircraft = fleet[2],
                DepartureUtc = D(2, 10),
                ArrivalUtc = D(2, 11),
                Status = FlightStatus.Scheduled
            };

            db.Flights.AddRange(f1, f2, f3);

            // Optional sample booking (1 seat on FM101) so MinFare/SeatsSold show up
            var pax = new Passenger { FullName = "Sample Pax", PassportNo = "P0000001", Nationality = "OM", DOB = new DateTime(1995, 1, 1) };
            var bk = new Booking { Passenger = pax, BookingRef = "BSEED001", Status = BookingStatus.Confirmed, BookingDate = DateTime.UtcNow };
            var tk = new Ticket { Booking = bk, Flight = f1, SeatNumber = "S001", Fare = 39.50m, CheckedIn = false };
            db.AddRange(pax, bk, tk);

            await db.SaveChangesAsync();
        }
    }
}

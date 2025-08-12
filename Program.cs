using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using FlightApp.Data;
using FlightApp.Models;
using FlightApp.DTOs;
using FlightApp.Repositories;
using FlightApp.Services;

class Program
{
    static async Task Main()
    {
        var cs = @"Server=(localdb)\MSSQLLocalDB;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FlightDbContext>()
            .UseSqlServer(cs)
            .EnableSensitiveDataLogging()
            .Options;

        using var db = new FlightDbContext(options);

        // 1) Apply migrations
        Console.WriteLine("Applying migrations…");
        await db.Database.MigrateAsync();

        // 2) Seed one flight if empty
        await SeedIfEmptyAsync(db);

        // 3) Unique-seat constraint smoke test
        await DuplicateSeatSmokeTestAsync(db);

        // 4) Auth: register (if needed) + login
        var auth = new AuthService(db);
        try { await auth.RegisterAsync(new RegisterDto("Guest", "guest@example.com", "Secret123!", "Guest")); }
        catch (InvalidOperationException) { /* already exists */ }

        var session = await auth.LoginAsync(new LoginDto("guest@example.com", "Secret123!"));
        Console.WriteLine($"Logged in. Token: {session.Token}");

        // 5) Search flights for next 7 days MCT->DXB
        var flightSvc = new FlightService(new FlightRepository(db));
        var results = await flightSvc.SearchAsync(
            new FlightSearchRequest(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), "MCT", "DXB", null, null));

        foreach (var r in results)
            Console.WriteLine($"{r.FlightNumber} {r.OriginIata}->{r.DestIata} {r.DepartureUtc:u} MinFare:{r.MinFare}");

        Console.WriteLine("Search finished.");

        // 6) Book 2 seats on first result
        if (results.Any())
        {
            var bookingSvc = new BookingService(new BookingRepository(db), auth);
            var (ok, msg, newBooking) = await bookingSvc.BookAsync(
                session.Token, results.First().FlightId, 2,
                "Mohammed Tester", "P7654321", "OM", new DateTime(1999, 5, 1));

            Console.WriteLine(ok
                ? $"Booked! Ref={newBooking!.BookingRef}, Seats=[{string.Join(",", newBooking.Tickets.Select(t => t.SeatNumber))}]"
                : $"Booking failed: {msg}");

            // 7) List bookings for that passport
            var myBookings = await bookingSvc.GetBookingsByPassportAsync("P7654321");
            foreach (var b in myBookings)
            {
                var f = b.Tickets.FirstOrDefault()?.Flight;  // Booking -> Tickets[0] -> Flight
                var fn = f?.FlightNumber ?? "?";
                var o = f?.Route?.OriginAirport?.IATA ?? "?";
                var d = f?.Route?.DestinationAirport?.IATA ?? "?";
                Console.WriteLine($"PNR {b.BookingRef} -> {fn} {o}->{d} Seats:{b.Tickets.Count}");
            }
        }
        else
        {
            Console.WriteLine("No flights to book.");
        }

        Console.WriteLine("Done. Press any key to exit…");
        Console.ReadKey();
    }

    // -------- helpers --------

    static async Task SeedIfEmptyAsync(FlightDbContext db)
    {
        if (await db.Airports.AnyAsync()) return;

        var mct = new Airport { IATA = "MCT", Name = "Muscat Intl", City = "Muscat", Country = "Oman" };
        var dxb = new Airport { IATA = "DXB", Name = "Dubai Intl", City = "Dubai", Country = "UAE" };
        db.AddRange(mct, dxb);

        var route = new Route { OriginAirport = mct, DestinationAirport = dxb, DistanceKm = 347 };
        var ac = new Aircraft { TailNumber = "A9C-001", Model = "A320", Capacity = 180 };
        var fl = new Flight
        {
            FlightNumber = "FM101",
            Route = route,
            Aircraft = ac,
            DepartureUtc = DateTime.UtcNow.AddDays(1),
            ArrivalUtc = DateTime.UtcNow.AddDays(1).AddHours(1)
        };
        db.Add(fl);

        var pax = new Passenger { FullName = "Mohammed", PassportNo = "P1234567", Nationality = "OM", DOB = new DateTime(2000, 1, 1) };
        var bk = new Booking { Passenger = pax, BookingRef = "B000001", Status = BookingStatus.Confirmed };
        var tk = new Ticket { Booking = bk, Flight = fl, SeatNumber = "S001", Fare = 39.50m };
        db.AddRange(pax, bk, tk);

        var captain = new CrewMember { FullName = "Captain Ali", Role = CrewRole.Pilot, LicenseNo = "LIC-001" };
        db.Add(captain);
        db.Add(new FlightCrew { Flight = fl, Crew = captain, RoleOnFlight = "Captain" });

        await db.SaveChangesAsync();
        Console.WriteLine("Seeded initial data.");
    }

    static async Task DuplicateSeatSmokeTestAsync(FlightDbContext db)
    {
        try
        {
            var firstFlight = await db.Flights.OrderBy(f => f.FlightId).FirstAsync();
            var anyBooking = await db.Bookings.OrderBy(b => b.BookingId).FirstAsync();

            db.Tickets.Add(new Ticket
            {
                BookingId = anyBooking.BookingId,
                FlightId = firstFlight.FlightId,
                SeatNumber = "S001", // duplicate
                Fare = 39.50m
            });

            await db.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Expected unique constraint to fail for duplicate seat but it saved!");
        }
        catch (DbUpdateException)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Unique constraint works: duplicate seat rejected.");
            db.ChangeTracker.Clear(); // clear failed Added entity
        }
        catch (InvalidOperationException)
        {
            // nothing to test if DB is empty
        }
        finally { Console.ResetColor(); }
    }
}

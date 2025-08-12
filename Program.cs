using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore.Migrations;

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

        // 1) create schema:
        //    - if you have migrations, apply them
        //    - if you don't, just create the schema from the model
        Console.WriteLine("Creating/Migrating database…");
        // add: using Microsoft.EntityFrameworkCore;

        Console.WriteLine("Creating/Migrating database…");
        var haveMigrations = db.Database.GetMigrations().Any();
        if (haveMigrations)
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();


        // 2) seed minimal data if empty
        if (!db.Airports.Any())
        {
            var mct = new Airport { IATA = "MCT", Name = "Muscat Intl", City = "Muscat", Country = "Oman" };
            var dxb = new Airport { IATA = "DXB", Name = "Dubai Intl", City = "Dubai", Country = "UAE" };
            db.AddRange(mct, dxb);

            var route = new Route { OriginAirport = mct, DestinationAirport = dxb, DistanceKm = 347 };
            var ac = new Aircraft { TailNumber = "A9C-001", Model = "A320", Capacity = 180 };
            var flight = new Flight
            {
                FlightNumber = "FM101",
                Route = route,
                Aircraft = ac,
                DepartureUtc = DateTime.UtcNow.AddDays(1),
                ArrivalUtc = DateTime.UtcNow.AddDays(1).AddHours(1)
            };
            db.Add(flight);

            var pax = new Passenger
            {
                FullName = "Mohammed",
                PassportNo = "P1234567",
                Nationality = "OM",
                DOB = new DateTime(2000, 1, 1)
            };

            var booking = new Booking
            {
                Passenger = pax,
                BookingRef = "B000001",
                Status = BookingStatus.Confirmed
            };

            var t1 = new Ticket { Booking = booking, Flight = flight, SeatNumber = "S001", Fare = 39.50m };
            db.AddRange(pax, booking, t1);

            var captain = new CrewMember { FullName = "Captain Ali", Role = CrewRole.Pilot, LicenseNo = "LIC-001" };
            db.Add(captain);
            db.Add(new FlightCrew { Flight = flight, Crew = captain, RoleOnFlight = "Captain" });

            await db.SaveChangesAsync();
        }

        // 3) query back with includes
        var flights = await db.Flights
            .Include(f => f.Route).ThenInclude(r => r.OriginAirport)
            .Include(f => f.Route).ThenInclude(r => r.DestinationAirport)
            .Include(f => f.Aircraft)
            .Include(f => f.Tickets)
            .ToListAsync();

        Console.WriteLine($"Flights: {flights.Count}");
        foreach (var f in flights)
            Console.WriteLine($"{f.FlightNumber} {f.Route!.OriginAirport!.IATA}->{f.Route!.DestinationAirport!.IATA} seatsSold={f.Tickets.Count}");

        // 4) constraint test: duplicate seat on same flight -> should fail
        var firstFlight = flights.First();
        var anyBooking = await db.Bookings.FirstAsync();
        db.Tickets.Add(new Ticket
        {
            BookingId = anyBooking.BookingId,
            FlightId = firstFlight.FlightId,
            SeatNumber = "S001", // duplicate
            Fare = 39.50m
        });

        try
        {
            await db.SaveChangesAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Expected unique constraint to fail for duplicate seat but it saved!");
        }
        catch (DbUpdateException)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Unique constraint works: duplicate seat rejected.");
        }
        finally
        {
            Console.ResetColor();
        }

        Console.WriteLine("Smoke test finished.");
        await db.Database.MigrateAsync();
    }
}

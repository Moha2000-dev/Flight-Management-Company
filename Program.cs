using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using FlightApp.Data;
using FlightApp.Models;
using FlightApp.DTOs;
using FlightApp.Repositories;
using FlightApp.Services;
using FlightApp.UI; // ConsoleUi

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

        // 2) Seed one flight if DB is empty
        await SeedIfEmptyAsync(db);

        // 3) Wire services
        IAuthService authSvc = new AuthService(db);
        IFlightService flightSvc = new FlightService(new FlightRepository(db));

        IBookingService booking = new BookingService(new BookingRepository(db), authSvc);
        IAdminService admin = new AdminService(db, authSvc);

        // Optional: ensure there’s an admin user you can log in with quickly
        try { await authSvc.RegisterAsync(new RegisterDto("Admin User", "admin@example.com", "Admin123!", "Admin")); }
        catch { /* already exists */ }

        // 4) Launch console UI (Login/Register → Guest/Admin menus)
        await new ConsoleUi(authSvc, flightSvc, booking, admin).RunAsync();
    }

    // ---- helpers ----
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
            ArrivalUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = FlightStatus.Scheduled
        };
        db.Add(fl);

        var pax = new Passenger { FullName = "Mohammed", PassportNo = "P1234567", Nationality = "OM", DOB = new DateTime(2000, 1, 1) };
        var bk = new Booking { Passenger = pax, BookingRef = "B000001", Status = BookingStatus.Confirmed, BookingDate = DateTime.UtcNow };
        var tk = new Ticket { Booking = bk, Flight = fl, SeatNumber = "S001", Fare = 39.50m, CheckedIn = false };

        db.AddRange(pax, bk, tk);

        var captain = new CrewMember { FullName = "Captain Ali", Role = CrewRole.Pilot, LicenseNo = "LIC-001" };
        db.Add(captain);
        db.Add(new FlightCrew { Flight = fl, Crew = captain, RoleOnFlight = "Captain" });

        await db.SaveChangesAsync();
        Console.WriteLine("Seeded initial data.");
    }
}

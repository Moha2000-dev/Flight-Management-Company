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
using FlightApp.SeedData;
class Program
{
    static async Task Main()
    {
        var cs = @"Server=(localdb)\MSSQLLocalDB;Database=FlightDB;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<FlightDbContext>()
            .UseSqlServer(cs)
            .EnableSensitiveDataLogging()
            .Options;

        using var db = new FlightDbContext(options);

        // 1) Apply migrations
        Console.WriteLine("Applying migrations…");
        await db.Database.MigrateAsync();

        // 2) Seed one flight if DB is empty
        await SeedData.EnsureSeededAsync(db);

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

  
}

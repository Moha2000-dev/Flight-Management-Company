using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using FlightApp.Data;
using FlightApp.UI;
using FlightApp.SeedData;

// Repos
using FlightApp.Repositories;
// Services
using FlightApp.Services;

class Program
{
    static async Task Main()
    {
        var cs = @"Server=(localdb)\MSSQLLocalDB;Database=FlightDB;Trusted_Connection=True;TrustServerCertificate=True";

        var services = new ServiceCollection();

        // DbContext
        services.AddDbContext<FlightDbContext>(o =>
            o.UseSqlServer(cs)
             .EnableSensitiveDataLogging() // optional for debugging
        );

        // Generic repo
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Per-entity repos (ensure these exist; comment any you haven’t created yet)
        services.AddScoped<IAirportRepository, AirportRepository>();
        services.AddScoped<IAircraftRepository, AircraftRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IPassengerRepository, PassengerRepository>();
        services.AddScoped<ICrewRepository, CrewRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IBaggageRepository, BaggageRepository>();
        services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAdminService, AdminService>();

        // Console UI must be scoped (it uses scoped services)
        services.AddScoped<ConsoleUi>();

        var provider = services.BuildServiceProvider();

        // migrate + seed in its own scope
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            try
            {
                await db.Database.MigrateAsync();
                await SeedData.EnsureSeededAsync(db);
                Console.WriteLine(" done DB migrated & seeded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Seeding failed: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        // run UI in a scope (so it can consume scoped services safely)
        using (var scope = provider.CreateScope())
        {
            var ui = scope.ServiceProvider.GetRequiredService<ConsoleUi>();
            await ui.RunAsync();
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FlightApp.SeedData; // at top

using FlightApp.Data;
using FlightApp.UI;
using FlightApp.SeedData;
// Repos
using FlightApp.Repositories;
// Services
using FlightApp.Services;
using System.Data;

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
        // DbContext factory (for migrations, etc.)
        services.AddSingleton<Func<IDbConnection>>(_ =>
        {
            var cs = @"Server=(localdb)\MSSQLLocalDB;Database=FlightDB;Trusted_Connection=True;TrustServerCertificate=True";
            return () => new Microsoft.Data.SqlClient.SqlConnection(cs);
        });

        // Program.cs
        services.AddDbContext<FlightDbContext>(opt =>
            opt.UseLazyLoadingProxies()
               .UseSqlServer(cs));


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

                // ⬇️ add this line to generate some delayed arrivals for testing
                await DemoData.DelaysAsync(db, count: 40, minDelayMin: 5, maxDelayMin: 45);
                await DemoData.MakeHighOccupancyAsync(db, minPercent: 85, flightsToBoost: 8);      // Task #4 fuel
                await DemoData.MakeConnectionItinerariesAsync(db, layoverHours: 3, itineraries: 10);                                                           // optional:
                await DemoData.DelaysAsync(db, count: 15, minDelayMin: 10, maxDelayMin: 45);
                await FlightApp.SeedData.DemoData.SeedHighOccupancyAsync(db,minPercent: 85, daysWindow: 7, howManyFlights: 8);// nice for Task #3
                Console.WriteLine(" DB migrated, seeded & delays injected.");
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

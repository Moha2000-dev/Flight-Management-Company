using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using FlightApp.Data;
using FlightApp.Repositories;
using FlightApp.Services;
using FlightApp.UI;
using FlightApp.SeedData;
using Microsoft.Data.SqlClient;

class Program
{
    static async Task Main()
    {
        var cs = @"Server=(localdb)\MSSQLLocalDB;Database=FlightDB;Trusted_Connection=True;TrustServerCertificate=True";

        var services = new ServiceCollection();

        // DbContext (single registration)
        services.AddDbContext<FlightDbContext>(opt =>
    opt.UseLazyLoadingProxies()
       .UseSqlServer(cs)); // debug only

        // Only keep this if you'll use Dapper; otherwise delete
        services.AddSingleton<Func<IDbConnection>>(_ => () => new SqlConnection(cs));

        // Generic + per-entity repos
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
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

        // UI
        services.AddScoped<ConsoleUi>();

        var provider = services.BuildServiceProvider();

        // Migrate + seed + demo data
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            try
            {
                await db.Database.MigrateAsync();
                await SeedData.EnsureSeededAsync(db);

                // --- Demo data to exercise the Tasks menu ---
                await DemoData.DelaysAsync(db, count: 40, minDelayMin: 5, maxDelayMin: 45);  // create delayed arrivals
                await DemoData.SeedHighOccupancyAsync(db, minPercent: 85, daysWindow: 7, howManyFlights: 8); // push some flights >=85%
                await DemoData.MakeConnectionItinerariesAsync(db, layoverHours: 3, itineraries: 10);          // create connecting bookings

                Console.WriteLine(" DB migrated, seeded & demo data injected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Seeding failed: {ex.Message}");
                Console.WriteLine(ex);
            }
        }

        // Run app
        using (var scope = provider.CreateScope())
        {
            var ui = scope.ServiceProvider.GetRequiredService<ConsoleUi>();
            await ui.RunAsync();
        }
    }
}

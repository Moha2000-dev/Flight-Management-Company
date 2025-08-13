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
        services.AddDbContext<FlightDbContext>(o => o.UseSqlServer(cs));

        // Generic repo
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Per-entity repos (register all you have)
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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAdminService, AdminService>();

        // Console UI
        services.AddSingleton<ConsoleUi>();

        var provider = services.BuildServiceProvider();

        // migrate + seed
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
            await db.Database.MigrateAsync();
            await SeedData.EnsureSeededAsync(db);
        }

        // run UI
        await provider.GetRequiredService<ConsoleUi>().RunAsync();
    }
}

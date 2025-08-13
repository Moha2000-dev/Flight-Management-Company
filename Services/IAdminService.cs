using System;
using System.Threading.Tasks;

namespace FlightApp.Services
{
    public interface IAdminService
    {
        Task AddAirportAsync(string token, string iata, string name, string city, string country, string? timeZone = null);
        Task<int> AddRouteAsync(string token, string originIata, string destIata, int distanceKm);
        Task AddAircraftAsync(string token, string tail, string model, int capacity);
        Task<int> AddFlightAsync(string token, string flightNo, string originIata, string destIata, DateTime depUtc, DateTime arrUtc, string tail);

        Task<int> AddMaintenanceAsync(string token, string tail, string workType, string? notes, bool grounds);
        Task CompleteMaintenanceAsync(string token, int maintenanceId);
    }
}

using FlightApp.DTOs;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);

        Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc);
        Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN);
        Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent);

        // Return ONE aggregate for a flight’s availability (not a list of strings)
        Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId);

        // Use the DTO name you actually have. If your DTO file says BaggageOverweightDto,
        // use that here and in the repo/service.
        Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg);
    }
}

using FlightApp.DTOs;

namespace FlightApp.Services
{
    public interface IFlightService
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);

        Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc);
        Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN);
        Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent);
        Task<List<string>> GetAvailableSeatsAsync(int flightId);
        Task<List<BaggageOverweightDto>> GetOverweightBaggageAsync(decimal limitKg);
    }
}
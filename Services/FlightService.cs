using FlightApp.DTOs;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class FlightService : IFlightService
    {
        private readonly IFlightRepository _repo;
        public FlightService(IFlightRepository repo) { _repo = repo; }

        public Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req) => _repo.SearchAsync(req);
        public Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc) => _repo.GetDailyManifestAsync(dayUtc);
        public Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN) => _repo.GetTopRoutesByRevenueAsync(fromUtc, toUtc, topN);
        public Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent) => _repo.GetHighOccupancyAsync(fromUtc, toUtc, minPercent);
        public Task<List<string>> GetAvailableSeatsAsync(int flightId) => _repo.GetAvailableSeatsAsync(flightId);
        public Task<List<BaggageOverweightDto>> GetOverweightBaggageAsync(decimal limitKg) => _repo.GetOverweightBaggageAsync(limitKg);
    }
}

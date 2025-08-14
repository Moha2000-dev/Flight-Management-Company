using FlightApp.DTOs;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class FlightService : IFlightService
    {

        private readonly IFlightRepository _repo;
        public FlightService(IFlightRepository repo) => _repo = repo;
        public Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req)
            => _repo.SearchAsync(req);

        public Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc)
            => _repo.GetDailyManifestAsync(dayUtc);

        public Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN)
         => _repo.GetTopRoutesByRevenueAsync(fromUtc, toUtc, topN);

        public Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent)
            => _repo.GetHighOccupancyAsync(fromUtc, toUtc, minPercent);

        public Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId)
            => _repo.GetAvailableSeatsAsync(flightId);

        public Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg)
            => _repo.GetOverweightBagsAsync(thresholdKg);

        // already present in your project – keep them
        // FlightService
        public Task<List<OnTimePerfDto>> OnTimePerformanceAsync(DateTime fromUtc, DateTime toUtc, int toleranceMinutes, bool byRoute = true)
            => _repo.GetOnTimePerformanceAsync(fromUtc, toUtc, toleranceMinutes, byRoute);


        public Task<List<CrewConflictDto>> CrewConflictsAsync(DateTime fromUtc, DateTime toUtc)
            => _repo.GetCrewConflictsAsync(fromUtc, toUtc);

        public Task<List<FrequentFlierDto>> FrequentFliersAsync(int topN, bool byFlights = true)
            => _repo.GetFrequentFliersAsync(topN, byFlights);

        public Task<List<MaintenanceAlertDto>> MaintenanceAlertsAsync(int distanceThresholdKm, int olderThanDays)
            => _repo.GetMaintenanceAlertsAsync(distanceThresholdKm, olderThanDays);

        public Task<List<BaggageOverweightAlertDto>> BaggageOverweightAlertsAsync(decimal perTicketLimitKg)
            => _repo.GetBaggageOverweightAlertsAsync(perTicketLimitKg);

        public Task<PagedFlightsDto> FlightsPageAsync(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc)
            => _repo.GetFlightsPageAsync(page, pageSize, fromUtc, toUtc);

        public Task<Dictionary<string, int>> FlightsToDictionaryAsync()
            => _repo.ToDictionaryFlightsByNumberAsync();

        public Task<string[]> TopRouteCodesArrayAsync(int topN, DateTime fromUtc, DateTime toUtc)
            => _repo.ToArrayTopRouteCodesAsync(topN, fromUtc, toUtc);

        public Task<List<DailyRevenueDto>> DailyRevenueRunningAsync(int daysBack)
            => _repo.GetDailyRevenueRunningAsync(daysBack);

        public Task<List<ForecastDto>> ForecastNextWeekAsync()
            => _repo.GetNextWeekForecastAsync();

        // NEW (Task 7)
        public Task<List<PassengerConnectionDto>> PassengersWithConnectionsAsync(int maxLayoverHours)
            => _repo.GetPassengersWithConnectionsAsync(maxLayoverHours);

        public Task<List<PassengerConnectionDto>> GetPassengersWithConnectionsAsync(int maxLayoverHours)
            => _repo.GetPassengersWithConnectionsAsync(maxLayoverHours);
    }
}

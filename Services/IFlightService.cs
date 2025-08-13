using FlightApp.DTOs;

namespace FlightApp.Services
{
    public interface IFlightService
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);

  
        Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg);


        Task<List<OnTimePerfDto>> OnTimePerformanceAsync(DateTime fromUtc, DateTime toUtc, int toleranceMinutes, bool byRoute = true);
        Task<List<CrewConflictDto>> CrewConflictsAsync(DateTime fromUtc, DateTime toUtc);
        Task<List<FrequentFlierDto>> FrequentFliersAsync(int topN, bool byFlights = true);
        Task<List<MaintenanceAlertDto>> MaintenanceAlertsAsync(int distanceThresholdKm, int olderThanDays);
        Task<List<BaggageOverweightAlertDto>> BaggageOverweightAlertsAsync(decimal perTicketLimitKg);
        Task<PagedFlightsDto> FlightsPageAsync(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc);
        Task<Dictionary<string, int>> FlightsToDictionaryAsync();
        Task<string[]> TopRouteCodesArrayAsync(int topN, DateTime fromUtc, DateTime toUtc);
        Task<List<DailyRevenueDto>> DailyRevenueRunningAsync(int daysBack);
        Task<List<ForecastDto>> ForecastNextWeekAsync();

        Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc);
        Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN);
        Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent);
        Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId);
        Task<List<PassengerConnectionDto>> PassengersWithConnectionsAsync(int maxLayoverHours);

    }
}

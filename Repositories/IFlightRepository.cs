using FlightApp.DTOs;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);

        // Use the DTO name you actually have. If your DTO file says BaggageOverweightDto,
        // use that here and in the repo/service.
        Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg);




        Task<List<OnTimePerfDto>> GetOnTimePerformanceAsync(DateTime fromUtc, DateTime toUtc, int toleranceMinutes, bool byRoute = true);

        Task<List<CrewConflictDto>> GetCrewConflictsAsync(DateTime fromUtc, DateTime toUtc);

        Task<List<FrequentFlierDto>> GetFrequentFliersAsync(int topN, bool byFlights = true);

        Task<List<MaintenanceAlertDto>> GetMaintenanceAlertsAsync(int distanceThresholdKm, int olderThanDays);

        Task<List<BaggageOverweightAlertDto>> GetBaggageOverweightAlertsAsync(decimal perTicketLimitKg);

        Task<PagedFlightsDto> GetFlightsPageAsync(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc);

        Task<Dictionary<string, int>> ToDictionaryFlightsByNumberAsync();

        Task<string[]> ToArrayTopRouteCodesAsync(int topN, DateTime fromUtc, DateTime toUtc);

        Task<List<DailyRevenueDto>> GetDailyRevenueRunningAsync(int daysBack);

        Task<List<ForecastDto>> GetNextWeekForecastAsync();

        Task<List<FlightManifestDto>> GetDailyManifestAsync(DateTime dayUtc);
        Task<List<RouteRevenueDto>> GetTopRoutesByRevenueAsync(DateTime fromUtc, DateTime toUtc, int topN);
        Task<List<SeatOccupancyDto>> GetHighOccupancyAsync(DateTime fromUtc, DateTime toUtc, int minPercent);
        Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId);
        Task<List<PassengerConnectionDto>> GetPassengersWithConnectionsAsync(int maxLayoverHours);

    }
}

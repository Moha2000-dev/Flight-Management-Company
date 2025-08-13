using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<AvailableSeatsDto?> GetAvailableSeatsAsync(int flightId);
        Task<List<OverweightBagDto>> GetOverweightBagsAsync(decimal thresholdKg);

        // “Tasks” items
        Task<List<OnTimePerfDto>> GetOnTimePerformanceAsync(DateTime fromUtc, DateTime toUtc, int toleranceMinutes, bool byRoute);
        Task<List<CrewConflictDto>> GetCrewConflictsAsync(DateTime fromUtc, DateTime toUtc);
   
        Task<List<FrequentFlierDto>> GetFrequentFliersAsync(int topN, bool byFlights);
        Task<List<MaintenanceAlertDto>> GetMaintenanceAlertsAsync(int distanceThresholdKm, int olderThanDays);
        Task<List<BaggageOverweightAlertDto>> GetBaggageOverweightAlertsAsync(decimal perTicketLimitKg);
        Task<PagedFlightsDto> GetFlightsPageAsync(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc);
        Task<Dictionary<string, int>> ToDictionaryFlightsByNumberAsync();
        Task<string[]> ToArrayTopRouteCodesAsync(int topN, DateTime fromUtc, DateTime toUtc);
        Task<List<DailyRevenueDto>> GetDailyRevenueRunningAsync(int daysBack);
        Task<List<ForecastDto>> GetNextWeekForecastAsync();
        Task<List<PassengerConnectionDto>> GetPassengersWithConnectionsAsync(int maxLayoverHours);
    }
}

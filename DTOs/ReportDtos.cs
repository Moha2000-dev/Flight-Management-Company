using System;
using System.Collections.Generic;

namespace FlightApp.DTOs
{
    public record FlightManifestDto(
        int FlightId,
        string FlightNumber,
        string OriginIata,
        string DestIata,
        DateTime DepartureUtc,
        int TicketsSold);

    public record RouteRevenueDto(
        string OriginIata,
        string DestIata,
        int Flights,
        int Tickets,
        decimal Revenue);

    public record SeatOccupancyDto(
        int FlightId,
        string FlightNumber,
        DateTime DepartureUtc,
        int Capacity,
        int SeatsSold,
        int Percent);

    // SeatList is optional -> fixes CS7036 when you only pass summary
    public record AvailableSeatsDto(
        int FlightId,
        string FlightNumber,
        int Capacity,
        int SeatsSold,
        int SeatsAvailable,
        List<string>? SeatList = null);

    public record OverweightBagDto(
        string BookingRef,
        string TagNumber,
        decimal WeightKg,
        string FlightNumber,
        string OriginIata,
        string DestIata);

    public record OnTimePerfDto(
        string Key,
        int Flights,
        int OnTime,
        int Late,
        int PctOnTime);

    public record CrewConflictDto(
        int CrewId,
        string CrewName,
        int FlightAId,
        int FlightBId,
        DateTime A_DepartureUtc,
        DateTime B_DepartureUtc);

    public record FrequentFlierDto(
        string FullName,
        int Flights,
        int DistanceKm);

    public record MaintenanceAlertDto(
        string TailNumber,
        int Flights,
        int DistanceKm,
        DateTime? LastCompletedUtc,
        bool NeedsAttention);

    public record BaggageOverweightAlertDto(
        string BookingRef,
        string PassengerName,
        string FlightNumber,
        decimal TotalWeightKg);

    public record FlightListRowDto(
        int FlightId,
        string FlightNumber,
        string OriginIata,
        string DestIata,
        DateTime DepartureUtc);

    public record PagedFlightsDto(
        int Page,
        int PageSize,
        int Total,
        List<FlightListRowDto> Rows);

    public record DailyRevenueDto(
        DateTime DayUtc,
        decimal Revenue,
        decimal RunningTotal);

    public record ForecastDto(
        DateTime DayUtc,
        int ExpectedTickets);

    public record PassengerConnectionDto(
      string BookingRef,
      string PassengerName,
      string FlightA,
      string FlightB,
      string OriginIata,
      string ViaIata,
      string DestIata,
      DateTime A_DepartureUtc,
      DateTime A_ArrivalUtc,
      DateTime B_DepartureUtc,
      int LayoverMinutes);
}

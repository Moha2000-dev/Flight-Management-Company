namespace FlightApp.DTOs
{
    // Report 1: Manifest
    public record FlightManifestDto(
        int FlightId,
        string FlightNumber,
        string Origin,
        string Dest,
        DateTime DepartureUtc,
        int TicketsSold);

    // Report 2: Top routes by revenue
    public record RouteRevenueDto(
        string OriginIata,
        string DestIata,
        int Flights,
        int Tickets,
        decimal Revenue);

    // Report 3: High occupancy
    public record SeatOccupancyDto(
        int FlightId,
        string FlightNumber,
        DateTime DepartureUtc,
        int Capacity,
        int SeatsSold,
        int Percent);

    // Report 4: Available seats for a flight
    public record AvailableSeatsDto(
        int FlightId,
        string FlightNumber,
        int Capacity,
        int SeatsSold,
        int SeatsAvailable,
        List<string> SeatList
    );



    // Report 5: Overweight bags
    public record OverweightBagDto(
       string BookingRef,
       string TagNumber,
       decimal WeightKg,
       string FlightNumber,
       string OriginIata,
       string DestIata);


    // 3) On-time performance
    public record OnTimePerfDto(string Key, int Flights, int OnTime, int Late, int PctOnTime);

    // 6) Crew scheduling conflicts
    public record CrewConflictDto(int CrewId, string CrewName, int FlightAId, int FlightBId);

    // 8) Frequent fliers
    public record FrequentFlierDto(int PassengerId, string FullName, int Flights, int DistanceKm);

    // 9) Maintenance alert (distance ~ “hours” proxy)
    public record MaintenanceAlertDto(string TailNumber, int Flights, int DistanceKm, DateTime? LastCompletedUtc, bool NeedsAttention);

    // 10) Baggage overweight alerts (per ticket/passenger)
    public record BaggageOverweightAlertDto(string BookingRef, string PassengerName, string FlightNumber, decimal TotalWeightKg);

    // 11b) Paged flights (simple)
    public record PagedFlightsDto(int Page, int PageSize, int Total, List<FlightSearchDto> Rows);

    // 13) Running revenue
    public record DailyRevenueDto(DateTime DayUtc, decimal Revenue, decimal RunningTotal);

    // 14) Simple forecast
    public record ForecastDto(DateTime DayUtc, int ExpectedTickets);

    // 15) Passenger connections
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
        int LayoverMinutes
    );
}

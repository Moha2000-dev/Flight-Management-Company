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
        int FlightId, string FlightNumber, int Capacity, int SeatsSold, int SeatsAvailable);


    // Report 5: Overweight bags
    public record OverweightBagDto(
       string BookingRef,
       string TagNumber,
       decimal WeightKg,
       string FlightNumber,
       string OriginIata,
       string DestIata);
}

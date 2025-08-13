namespace FlightApp.DTOs
{
    public record FlightManifestDto(int FlightId, string FlightNumber, string Origin, string Dest,
                                    DateTime DepartureUtc, int TicketsSold);

    public record RouteRevenueDto(string Origin, string Dest, int Flights, int Tickets, decimal Revenue);

    public record SeatOccupancyDto(int FlightId, string FlightNumber, int Capacity, int Sold, int Percent);

    public record BaggageOverweightDto(string BookingRef, string TagNumber, decimal WeightKg, int FlightId);
}

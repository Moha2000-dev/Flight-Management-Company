using System;

namespace FlightApp.DTOs
{
    // Input to search
    public record FlightSearchRequest(
        DateTime? FromUtc,
        DateTime? ToUtc,
        string? OriginIata,
        string? DestIata,
        decimal? MinFare,
        decimal? MaxFare);

    // Output row
    public record FlightSearchDto(
        int FlightId,
        string FlightNumber,
        string OriginIata,
        string DestIata,
        DateTime DepartureUtc,
        DateTime ArrivalUtc,
        string AircraftModel,
        int Capacity,
        int SeatsSold,
        decimal MinFare);
}


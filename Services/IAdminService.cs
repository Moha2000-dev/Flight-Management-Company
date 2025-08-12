using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IAdminService
    {
        Task<Airport> AddAirportAsync(string token, string iata, string name, string city, string country, string timeZone);
        Task<Route> AddRouteAsync(string token, string originIata, string destIata, int distanceKm);
        Task<Aircraft> AddAircraftAsync(string token, string tail, string model, int capacity);
        Task<Flight> AddFlightAsync(string token, string flightNumber, string originIata, string destIata,
                                        DateTime depUtc, DateTime arrUtc, string tailNumber);
    }
}

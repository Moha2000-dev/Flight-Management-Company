using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly FlightDbContext _db;
        private readonly IAuthService _auth;
        public AdminService(FlightDbContext db, IAuthService auth) { _db = db; _auth = auth; }

        private async Task EnsureAdminAsync(string token)
        {
            var u = await _auth.ValidateTokenAsync(token) ?? throw new UnauthorizedAccessException("Invalid session.");
            if (u.Role != UserRole.Admin) throw new UnauthorizedAccessException("Admin only.");
        }

        public async Task<Airport> AddAirportAsync(string token, string iata, string name, string city, string country, string timeZone)
        {
            await EnsureAdminAsync(token);
            iata = iata.Trim().ToUpperInvariant();
            if (await _db.Airports.AnyAsync(a => a.IATA == iata)) throw new InvalidOperationException("IATA exists.");
            var a = new Airport { IATA = iata, Name = name, City = city, Country = country, TimeZone = timeZone };
            _db.Airports.Add(a); await _db.SaveChangesAsync(); return a;
        }

        public async Task<Route> AddRouteAsync(string token, string originIata, string destIata, int distanceKm)
        {
            await EnsureAdminAsync(token);
            var o = await _db.Airports.FirstOrDefaultAsync(a => a.IATA == originIata.ToUpper())
                    ?? throw new InvalidOperationException("Origin not found.");
            var d = await _db.Airports.FirstOrDefaultAsync(a => a.IATA == destIata.ToUpper())
                    ?? throw new InvalidOperationException("Destination not found.");
            var r = new Route { OriginAirportId = o.AirportId, DestinationAirportId = d.AirportId, DistanceKm = distanceKm };
            _db.Routes.Add(r); await _db.SaveChangesAsync(); return r;
        }

        public async Task<Aircraft> AddAircraftAsync(string token, string tail, string model, int capacity)
        {
            await EnsureAdminAsync(token);
            if (await _db.Aircraft.AnyAsync(a => a.TailNumber == tail)) throw new InvalidOperationException("Tail exists.");
            var ac = new Aircraft { TailNumber = tail, Model = model, Capacity = capacity };
            _db.Aircraft.Add(ac); await _db.SaveChangesAsync(); return ac;
        }

        public async Task<Flight> AddFlightAsync(string token, string flightNumber, string originIata, string destIata,
                                                 DateTime depUtc, DateTime arrUtc, string tailNumber)
        {
            await EnsureAdminAsync(token);
            var r = await _db.Routes
                .Include(x => x.OriginAirport).Include(x => x.DestinationAirport)
                .FirstOrDefaultAsync(x => x.OriginAirport!.IATA == originIata.ToUpper()
                                        && x.DestinationAirport!.IATA == destIata.ToUpper())
                ?? throw new InvalidOperationException("Route not found.");

            var ac = await _db.Aircraft.FirstOrDefaultAsync(a => a.TailNumber == tailNumber)
                     ?? throw new InvalidOperationException("Aircraft not found.");

            var f = new Flight
            {
                FlightNumber = flightNumber,
                RouteId = r.RouteId,
                AircraftId = ac.AircraftId,
                DepartureUtc = depUtc,
                ArrivalUtc = arrUtc,
                Status = FlightStatus.Scheduled
            };
            _db.Flights.Add(f); await _db.SaveChangesAsync(); return f;
        }
    }
}

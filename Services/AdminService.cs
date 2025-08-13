using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlightApp.Data;
using FlightApp.Models;

namespace FlightApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly FlightDbContext _db;
        private readonly IAuthService _auth;

        public AdminService(FlightDbContext db, IAuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        // ---- helpers --------------------------------------------------------

        private async Task<User> RequireAdminAsync(string token)
        {
            var user = await _auth.ValidateTokenAsync(token)
                       ?? throw new UnauthorizedAccessException("Invalid/expired session.");
            if (user.Role != UserRole.Admin)
                throw new UnauthorizedAccessException("Admin role required.");
            return user;
        }

        // ---- Airports -------------------------------------------------------

        // timeZone is optional: if your Airport model has no TimeZone column, we just ignore it.
        public async Task AddAirportAsync(string token, string iata, string name, string city, string country, string? timeZone = null)
        {
            await RequireAdminAsync(token);

            iata = iata.Trim().ToUpperInvariant();
            if (await _db.Airports.AnyAsync(a => a.IATA == iata))
                throw new InvalidOperationException("Airport already exists.");

            var a = new Airport { IATA = iata, Name = name.Trim(), City = city.Trim(), Country = country.Trim() };
            _db.Airports.Add(a);
            await _db.SaveChangesAsync();
        }

        // ---- Routes ---------------------------------------------------------

        public async Task<int> AddRouteAsync(string token, string originIata, string destIata, int distanceKm)
        {
            await RequireAdminAsync(token);

            var o = await _db.Airports.FirstOrDefaultAsync(a => a.IATA == originIata.ToUpper())
                    ?? throw new InvalidOperationException("Origin airport not found.");
            var d = await _db.Airports.FirstOrDefaultAsync(a => a.IATA == destIata.ToUpper())
                    ?? throw new InvalidOperationException("Destination airport not found.");

            if (await _db.Routes.AnyAsync(r => r.OriginAirportId == o.AirportId && r.DestinationAirportId == d.AirportId))
                throw new InvalidOperationException("Route already exists.");

            var r = new Route { OriginAirportId = o.AirportId, DestinationAirportId = d.AirportId, DistanceKm = distanceKm };
            _db.Routes.Add(r);
            await _db.SaveChangesAsync();
            return r.RouteId;
        }

        // ---- Aircraft -------------------------------------------------------

        public async Task AddAircraftAsync(string token, string tail, string model, int capacity)
        {
            await RequireAdminAsync(token);

            tail = tail.Trim().ToUpperInvariant();
            if (await _db.Aircraft.AnyAsync(a => a.TailNumber == tail))
                throw new InvalidOperationException("Tail already exists.");

            _db.Aircraft.Add(new Aircraft { TailNumber = tail, Model = model.Trim(), Capacity = capacity });
            await _db.SaveChangesAsync();
        }

        // ---- Flights --------------------------------------------------------

        public async Task<int> AddFlightAsync(string token, string flightNo, string originIata, string destIata,
                                              DateTime depUtc, DateTime arrUtc, string tail)
        {
            await RequireAdminAsync(token);

            var route = await _db.Routes
                .Include(r => r.OriginAirport)
                .Include(r => r.DestinationAirport)
                .FirstOrDefaultAsync(r => r.OriginAirport!.IATA == originIata.ToUpper()
                                       && r.DestinationAirport!.IATA == destIata.ToUpper())
                        ?? throw new InvalidOperationException("Route not found.");

            var ac = await _db.Aircraft.FirstOrDefaultAsync(a => a.TailNumber == tail.ToUpper())
                     ?? throw new InvalidOperationException("Aircraft not found.");

            var f = new Flight
            {
                FlightNumber = flightNo.Trim().ToUpperInvariant(),
                RouteId = route.RouteId,
                AircraftId = ac.AircraftId,
                DepartureUtc = DateTime.SpecifyKind(depUtc, DateTimeKind.Utc),
                ArrivalUtc = DateTime.SpecifyKind(arrUtc, DateTimeKind.Utc),
                Status = FlightStatus.Scheduled
            };

            _db.Flights.Add(f);
            await _db.SaveChangesAsync();
            return f.FlightId;
        }

        // ---- Maintenance ----------------------------------------------------

        public async Task<int> AddMaintenanceAsync(string token, string tail, string workType, string? notes, bool grounds)
        {
            await RequireAdminAsync(token);

            var ac = await _db.Aircraft.FirstOrDefaultAsync(a => a.TailNumber == tail.ToUpper())
                     ?? throw new InvalidOperationException("Aircraft not found.");

            var m = new AircraftMaintenance
            {
                AircraftId = ac.AircraftId,
                WorkType = string.IsNullOrWhiteSpace(workType) ? "Inspection" : workType.Trim(),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                ScheduledUtc = DateTime.UtcNow,
                CompletedUtc = null,
                GroundsAircraft = grounds
            };
            _db.Maintenances.Add(m);
            await _db.SaveChangesAsync();
            return m.AircraftMaintenanceId;
        }

        public async Task CompleteMaintenanceAsync(string token, int maintenanceId)
        {
            await RequireAdminAsync(token);

            var m = await _db.Maintenances.FirstOrDefaultAsync(x => x.AircraftMaintenanceId == maintenanceId)
                    ?? throw new InvalidOperationException("Maintenance not found.");

            m.CompletedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}

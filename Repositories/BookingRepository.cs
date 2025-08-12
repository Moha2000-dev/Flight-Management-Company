using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(FlightDbContext db) : base(db) { }

        public async Task<Passenger> GetOrCreatePassengerAsync(string fullName, string passportNo, string nationality, DateTime dob)
        {
            var p = await _db.Passengers.FirstOrDefaultAsync(x => x.PassportNo == passportNo);
            if (p != null) return p;

            p = new Passenger { FullName = fullName, PassportNo = passportNo, Nationality = nationality, DOB = dob };
            _db.Passengers.Add(p);
            await _db.SaveChangesAsync();
            return p;
        }

        public async Task<(int Capacity, HashSet<string> Taken)> GetSeatMapAsync(int flightId)
        {
            var flight = await _db.Flights.Include(f => f.Aircraft)
                         .FirstOrDefaultAsync(f => f.FlightId == flightId)
                         ?? throw new InvalidOperationException("Flight not found.");

            var taken = await _db.Tickets
                         .Where(t => t.FlightId == flightId)
                         .Select(t => t.SeatNumber)
                         .ToListAsync();

            return (flight.Aircraft!.Capacity, new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<decimal> GetMinFareAsync(int flightId)
        {
            var min = await _db.Tickets
                       .Where(t => t.FlightId == flightId)
                       .Select(t => (decimal?)t.Fare)
                       .MinAsync();
            return min ?? 0m;
        }

        public async Task<Booking> CreateBookingAsync(int passengerId, int flightId, IEnumerable<string> seatNumbers, decimal farePerSeat)
        {
            var booking = new Booking
            {
                PassengerId = passengerId,
                BookingRef = $"B{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                Status = BookingStatus.Confirmed,
                BookingDate = DateTime.UtcNow
            };

            foreach (var seat in seatNumbers)
                booking.Tickets.Add(new Ticket { FlightId = flightId, SeatNumber = seat, Fare = farePerSeat, CheckedIn = false });

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            return booking;
        }

        public Task<List<Booking>> GetByPassportAsync(string passportNo) =>
      _db.Bookings
        .Include(b => b.Passenger)
        .Include(b => b.Tickets)
          .ThenInclude(t => t.Flight)
            .ThenInclude(f => f!.Route)
              .ThenInclude(r => r!.OriginAirport)
        .Include(b => b.Tickets)
          .ThenInclude(t => t.Flight)
            .ThenInclude(f => f!.Route)
              .ThenInclude(r => r!.DestinationAirport)
        .Where(b => b.Passenger!.PassportNo == passportNo)
        .OrderByDescending(b => b.BookingDate)
        .AsNoTracking()
        .ToListAsync();

    }
}

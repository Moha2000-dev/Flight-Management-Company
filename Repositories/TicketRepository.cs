using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class TicketRepository : Repository<Ticket>, ITicketRepository
    {
        public TicketRepository(FlightDbContext db) : base(db) { }

        public async Task<HashSet<string>> GetTakenSeatsAsync(int flightId)
        {
            var seats = await _db.Tickets
                .Where(t => t.FlightId == flightId)
                .Select(t => t.SeatNumber)
                .ToListAsync();

            return new HashSet<string>(seats, StringComparer.OrdinalIgnoreCase);
        }

        public Task<List<Ticket>> GetByBookingAsync(int bookingId) =>
            _db.Tickets.Where(t => t.BookingId == bookingId).ToListAsync();
    }
}

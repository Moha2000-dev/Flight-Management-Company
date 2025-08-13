using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface ITicketRepository : IRepository<Ticket>
    {
        Task<HashSet<string>> GetTakenSeatsAsync(int flightId);
        Task<List<Ticket>> GetByBookingAsync(int bookingId);
    }
}

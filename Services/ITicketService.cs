using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface ITicketService : ICrudService<Ticket>
    {
        Task<HashSet<string>> GetTakenSeatsAsync(int flightId);
        Task<List<Ticket>> GetByBookingAsync(int bookingId);
    }
}

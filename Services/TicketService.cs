using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class TicketService : CrudService<Ticket>, ITicketService
    {
        private readonly ITicketRepository _tickets;
        public TicketService(ITicketRepository tickets) : base(tickets) { _tickets = tickets; }
        public Task<HashSet<string>> GetTakenSeatsAsync(int flightId) => _tickets.GetTakenSeatsAsync(flightId);
        public Task<List<Ticket>> GetByBookingAsync(int bookingId) => _tickets.GetByBookingAsync(bookingId);
    }
}
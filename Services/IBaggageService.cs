using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IBaggageService : ICrudService<Baggage>
    {
        Task<List<Baggage>> GetByTicketAsync(int ticketId);
    }
}

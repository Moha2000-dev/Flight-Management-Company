using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IBaggageRepository : IRepository<Baggage>
    {
        Task<List<Baggage>> GetByTicketAsync(int ticketId);
    }
}

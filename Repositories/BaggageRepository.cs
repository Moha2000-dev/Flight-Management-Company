using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class BaggageRepository : Repository<Baggage>, IBaggageRepository
    {
        public BaggageRepository(FlightDbContext db) : base(db) { }
        public Task<List<Baggage>> GetByTicketAsync(int ticketId) =>
            _db.Baggage.Where(b => b.TicketId == ticketId).ToListAsync();
    }
}

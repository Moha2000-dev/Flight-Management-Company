using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class BaggageService : CrudService<Baggage>, IBaggageService
    {
        private readonly IBaggageRepository _baggage;
        public BaggageService(IBaggageRepository baggage) : base(baggage) { _baggage = baggage; }
        public Task<List<Baggage>> GetByTicketAsync(int ticketId) => _baggage.GetByTicketAsync(ticketId);
    }
}
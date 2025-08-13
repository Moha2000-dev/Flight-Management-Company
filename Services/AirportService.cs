using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class AirportService : CrudService<Airport>, IAirportService
    {
        private readonly IAirportRepository _airports;
        public AirportService(IAirportRepository airports) : base(airports) { _airports = airports; }
        public Task<Airport?> GetByIataAsync(string iata) => _airports.GetByIataAsync(iata);
    }
}
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IAirportService : ICrudService<Airport>
    {
        Task<Airport?> GetByIataAsync(string iata);
    }
}
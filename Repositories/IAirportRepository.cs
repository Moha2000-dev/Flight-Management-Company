using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IAirportRepository : IRepository<Airport>
    {
        Task<Airport?> GetByIataAsync(string iata);
        Task<bool> ExistsIataAsync(string iata);
    }
}

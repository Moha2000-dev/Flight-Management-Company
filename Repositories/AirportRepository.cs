using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class AirportRepository : Repository<Airport>, IAirportRepository
    {
        public AirportRepository(FlightDbContext db) : base(db) { }
        public Task<Airport?> GetByIataAsync(string iata) => _db.Airports.FirstOrDefaultAsync(a => a.IATA == iata);
        public Task<bool> ExistsIataAsync(string iata) => _db.Airports.AnyAsync(a => a.IATA == iata);
    }
}

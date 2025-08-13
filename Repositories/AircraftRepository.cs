using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class AircraftRepository : Repository<Aircraft>, IAircraftRepository
    {
        public AircraftRepository(FlightDbContext db) : base(db) { }
        public Task<Aircraft?> GetByTailAsync(string tailNumber) =>
            _db.Aircraft.FirstOrDefaultAsync(a => a.TailNumber == tailNumber);
    }
}

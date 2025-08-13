using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{
    public class PassengerRepository : Repository<Passenger>, IPassengerRepository
    {
        public PassengerRepository(FlightDbContext db) : base(db) { }
        public Task<Passenger?> FindByPassportAsync(string passportNo) =>
            _db.Passengers.FirstOrDefaultAsync(p => p.PassportNo == passportNo);
    }
}

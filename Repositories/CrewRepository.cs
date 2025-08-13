using FlightApp.Data;
using FlightApp.Models;

using FlightApp.Data;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public class CrewRepository : Repository<CrewMember>, ICrewRepository
    {
        public CrewRepository(FlightDbContext db) : base(db) { }
    }
}

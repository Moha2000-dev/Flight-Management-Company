using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Data;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Repositories
{  
    public class MaintenanceRepository : Repository<AircraftMaintenance>, IMaintenanceRepository
    {
        public MaintenanceRepository(FlightDbContext db) : base(db) { }

        /// Retrieves all open maintenance records for a specific aircraft tail number.
        public Task<List<AircraftMaintenance>> GetOpenForTailAsync(string tailNumber) =>
            _db.Maintenances
               .Include(m => m.Aircraft)
               .Where(m => m.Aircraft!.TailNumber == tailNumber && m.CompletedUtc == null)
               .ToListAsync();

    }
}

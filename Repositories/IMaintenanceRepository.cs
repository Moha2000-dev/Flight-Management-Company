using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IMaintenanceRepository : IRepository<AircraftMaintenance>
    {
        Task<List<AircraftMaintenance>> GetOpenForTailAsync(string tailNumber);
    }
}

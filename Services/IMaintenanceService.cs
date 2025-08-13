using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IMaintenanceService : ICrudService<AircraftMaintenance>
    {
        Task<List<AircraftMaintenance>> GetOpenForTailAsync(string tailNumber);
    }
}
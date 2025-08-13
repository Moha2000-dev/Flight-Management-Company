using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class MaintenanceService : CrudService<AircraftMaintenance>, IMaintenanceService
    {
        private readonly IMaintenanceRepository _maint;
        public MaintenanceService(IMaintenanceRepository maint) : base(maint) { _maint = maint; }
        public Task<List<AircraftMaintenance>> GetOpenForTailAsync(string tailNumber) =>
            _maint.GetOpenForTailAsync(tailNumber);
    }
}
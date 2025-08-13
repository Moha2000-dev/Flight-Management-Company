using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class AircraftService : CrudService<Aircraft>, IAircraftService
    {
        private readonly IAircraftRepository _aircraft;
        public AircraftService(IAircraftRepository aircraft) : base(aircraft) { _aircraft = aircraft; }
        public Task<Aircraft?> GetByTailAsync(string tailNumber) => _aircraft.GetByTailAsync(tailNumber);
    }
}
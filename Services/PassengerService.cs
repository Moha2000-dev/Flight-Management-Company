using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class PassengerService : CrudService<Passenger>, IPassengerService
    {
        private readonly IPassengerRepository _passengers;
        public PassengerService(IPassengerRepository passengers) : base(passengers) { _passengers = passengers; }
        public Task<Passenger?> FindByPassportAsync(string passportNo) => _passengers.FindByPassportAsync(passportNo);
    }
}
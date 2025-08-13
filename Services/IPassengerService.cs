using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IPassengerService : ICrudService<Passenger>
    {
        Task<Passenger?> FindByPassportAsync(string passportNo);
    }
}

using System.Threading.Tasks;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IPassengerRepository : IRepository<Passenger>
    {
        Task<Passenger?> FindByPassportAsync(string passportNo);
    }
}

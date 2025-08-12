using FlightApp.DTOs;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightApp.DTOs.FlightSearchRequest req);
    }
}

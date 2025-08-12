using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.DTOs;
using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IFlightRepository : IRepository<Flight>
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);
    }
}

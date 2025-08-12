using FlightApp.DTOs;

namespace FlightApp.Services
{
    public interface IFlightService
    {
        Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req);
    }
}

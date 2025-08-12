using System.Collections.Generic;
using System.Threading.Tasks;
using FlightApp.DTOs;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class FlightService : IFlightService
    {
        private readonly IFlightRepository _repo;
        public FlightService(IFlightRepository repo) { _repo = repo; }
        public Task<List<FlightSearchDto>> SearchAsync(FlightSearchRequest req) => _repo.SearchAsync(req);
    }
}

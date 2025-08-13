using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class CrewService : CrudService<CrewMember>, ICrewService
    {
        public CrewService(ICrewRepository repo) : base(repo) { }
    }
}
using System.ComponentModel.DataAnnotations;


namespace FlightApp.Models
{
    // From ERD: Route connects two Airports
    public class Route
    {
        [Key] public int RouteId { get; set; }

        [Required] public int OriginAirportId { get; set; }
        public Airport? OriginAirport { get; set; }

        [Required] public int DestinationAirportId { get; set; }
        public Airport? DestinationAirport { get; set; }

        public int DistanceKm { get; set; }

        public ICollection<Flight> Flights { get; set; } = new List<Flight>();
    }
}


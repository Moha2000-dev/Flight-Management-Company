using System.ComponentModel.DataAnnotations;


namespace FlightApp.Models
{
    // From ERD: Route connects two Airports
    public class Route
    {
        [Key] public int RouteId { get; set; }

        [Required] public int OriginAirportId { get; set; }
        public virtual Airport? OriginAirport { get; set; }
        public virtual Airport? DestinationAirport { get; set; }
        public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();

        [Required] public int DestinationAirportId { get; set; }
       

        public int DistanceKm { get; set; }

      
    }
}


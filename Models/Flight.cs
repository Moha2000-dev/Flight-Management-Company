using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    public enum FlightStatus { Scheduled, Departed, Landed, Canceled }

    // We’ll enforce (FlightNumber + DepartureDate) unique in DbContext.
    [Index(nameof(FlightNumber))]
    public class Flight
    {
        [Key] public int FlightId { get; set; }

        [Required, MaxLength(10)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required] public int RouteId { get; set; }
        public Route? Route { get; set; }

        [Required] public int AircraftId { get; set; }
        public Aircraft? Aircraft { get; set; }

        [Required] public DateTime DepartureUtc { get; set; }
        [Required] public DateTime ArrivalUtc { get; set; }

        [Required] public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<FlightCrew> FlightCrews { get; set; } = new List<FlightCrew>();

    }
}

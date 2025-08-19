using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    [Index(nameof(TailNumber), IsUnique = true)]
    public class Aircraft
    {
        [Key] public int AircraftId { get; set; }

        [Required, MaxLength(30)]
        public string TailNumber { get; set; } = string.Empty; // unique

        [Required, MaxLength(80)]
        public string Model { get; set; } = string.Empty;

        public int Capacity { get; set; } = 180;

        public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();
        public virtual ICollection<AircraftMaintenance> Maintenances { get; set; } = new List<AircraftMaintenance>();
    }
}

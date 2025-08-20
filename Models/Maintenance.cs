using System;
using System.ComponentModel.DataAnnotations;

namespace FlightApp.Models
{
    public class AircraftMaintenance
    {
        public int AircraftMaintenanceId { get; set; }

        // FK → Aircraft
        public int AircraftId { get; set; }
        public virtual Aircraft Aircraft { get; set; } = null!;

        [Required, MaxLength(100)]
        public string WorkType { get; set; } = "Inspection";

        [MaxLength(500)]
        public string? Notes { get; set; }

        // when it was reported/scheduled
        public DateTime ScheduledUtc { get; set; } = DateTime.UtcNow;

        //  when the task was finished (null means still open)
        public DateTime? CompletedUtc { get; set; }

        // optional flag if this grounds the aircraft
        public bool GroundsAircraft { get; set; }
       
    }
}

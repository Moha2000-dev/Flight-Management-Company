using System.ComponentModel.DataAnnotations;

namespace FlightApp.Models
{
    public class AircraftMaintenance
    {
        [Key] public int MaintenanceId { get; set; }

        [Required] public int AircraftId { get; set; }
        public Aircraft? Aircraft { get; set; }

        public DateTime MaintenanceDate { get; set; }
        [MaxLength(60)] public string Type { get; set; } = string.Empty;  // A-check, C-check, etc.
        [MaxLength(400)] public string? Notes { get; set; }
    }
}

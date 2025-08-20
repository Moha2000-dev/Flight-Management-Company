using System.ComponentModel.DataAnnotations;

namespace FlightApp.Models
{
    public enum CrewRole { Pilot, CoPilot, FlightAttendant }

    public class CrewMember
    {
        [Key] public int CrewId { get; set; }
        [Required, MaxLength(120)] public string FullName { get; set; } = string.Empty;
        [Required] public CrewRole Role { get; set; }
        [MaxLength(40)] public string? LicenseNo { get; set; }

        public virtual ICollection<FlightCrew> FlightCrews { get; set; } = new HashSet<FlightCrew>();
    }

    // Join table (many-to-many) between Flight and Crew
    // Composite key will be configured in DbContext.
    public class FlightCrew
    {
        public int FlightId { get; set; }
       

        public int CrewId { get; set; }
        
        // Optional: role on that specific flight (e.g., “Captain”)
        [MaxLength(40)] public string? RoleOnFlight { get; set; }

        public virtual Flight Flight { get; set; } = null!;
        public virtual CrewMember Crew { get; set; } = null!;
    }
}

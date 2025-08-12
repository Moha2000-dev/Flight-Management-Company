using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    //  Airport has IATA code, city, country, timezone
    [Index(nameof(IATA), IsUnique = true)]
    public class Airport
    {
        [Key] public int AirportId { get; set; }

        [Required, StringLength(3, MinimumLength = 3)]
        public string IATA { get; set; } = string.Empty;

        [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string City { get; set; } = string.Empty;
        [Required, MaxLength(120)] public string Country { get; set; } = string.Empty;
        [MaxLength(60)] public string TimeZone { get; set; } = "UTC";

        // navs
        public ICollection<Route> OriginRoutes { get; set; } = new List<Route>();
        public ICollection<Route> DestRoutes { get; set; } = new List<Route>();
    }
}

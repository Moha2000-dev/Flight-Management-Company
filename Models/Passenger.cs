using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    [Index(nameof(PassportNo), IsUnique = true)]
    public class Passenger
    {
        [Key] public int PassengerId { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PassportNo { get; set; } = string.Empty; // unique

        [MaxLength(3)]
        public string? Nationality { get; set; }

        public DateTime DOB { get; set; }

        // MUST be virtual for lazy-loading proxies
        public virtual ICollection<Booking> Bookings { get; set; } = new HashSet<Booking>();
    }
}

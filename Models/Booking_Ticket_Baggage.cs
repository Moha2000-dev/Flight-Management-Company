using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    public enum BookingStatus { Pending, Confirmed, Canceled }

    [Index(nameof(BookingRef), IsUnique = true)]
    public class Booking
    {
        [Key] public int BookingId { get; set; }

        [Required] public int PassengerId { get; set; }
  

        [Required, MaxLength(12)]
        public string BookingRef { get; set; } = string.Empty; // PNR/Ref

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public virtual Passenger? Passenger { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }

    // Each ticket belongs to a booking + flight
    // (FlightId, SeatNumber) must be unique – we’ll enforce in DbContext.
    public class Ticket
    {
        [Key] public int TicketId { get; set; }

        [Required] public int BookingId { get; set; }
        public virtual Booking? Booking { get; set; }

        [Required] public int FlightId { get; set; }
        public virtual Flight? Flight { get; set; }

        [Required, MaxLength(5)]
        public string SeatNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fare { get; set; }

        public bool CheckedIn { get; set; }

        public virtual ICollection<Baggage> Baggage { get; set; } = new List<Baggage>();
    }

    public class Baggage
    {
        [Key] public int BaggageId { get; set; }

        [Required] public int TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal WeightKg { get; set; }

        [MaxLength(20)]
        public string TagNumber { get; set; } = string.Empty;
    }
}

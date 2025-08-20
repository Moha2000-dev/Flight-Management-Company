using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    public enum UserRole { Admin, Agent, Guest }

    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key] public int UserId { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required] public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        [Required] public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        [Required] public UserRole Role { get; set; } = UserRole.Guest;

       
        public virtual ICollection<UserSession> Sessions { get; set; } = new HashSet<UserSession>();
    }

    [Index(nameof(Token), IsUnique = true)]
    public class UserSession
    {
        [Key] public int SessionId { get; set; }

        [Required, MaxLength(64)]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsActive { get; set; } = true;

        [Required] public int UserId { get; set; }


        public virtual User? User { get; set; }
    }
}

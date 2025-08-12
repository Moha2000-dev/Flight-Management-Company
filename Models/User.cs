using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Models
{
    public enum UserRole { Admin, Agent, Guest } // Represents different user roles in the system

    // App account (for login/token). One user can own many Passenger profiles if you want.
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key] public int UserId { get; set; }

        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(160)]
        public string Email { get; set; } = string.Empty;

        // Stored as salted+hashed bytes (we’ll implement hashing in AuthService later)
        [Required] public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        [Required] public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        [Required] public UserRole Role { get; set; } = UserRole.Guest;

        public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    }

    // Short-lived session token (so user doesn’t retype credentials)
    [Index(nameof(Token), IsUnique = true)]
    public class UserSession
    {
        [Key] public int SessionId { get; set; }
        [Required, MaxLength(64)] public string Token { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);
        public bool IsActive { get; set; } = true;

        [Required] public int UserId { get; set; }
        public User? User { get; set; }
    }
}

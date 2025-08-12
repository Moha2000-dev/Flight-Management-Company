using System.Security.Cryptography;
using FlightApp.Data;
using FlightApp.DTOs;
using FlightApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightApp.Services
{
    // Handles register/login, password hashing, and session tokens
    public class AuthService
    {
        private readonly FlightDbContext _db;
        public AuthService(FlightDbContext db) { _db = db; }

        // Strong salted hash (PBKDF2)
        private static void CreateHash(string password, out byte[] hash, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            hash = pbkdf2.GetBytes(32);
        }
        private static bool Verify(string password, byte[] hash, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var test = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(test, hash);
        }

        public async Task<SessionDto> RegisterAsync(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
                throw new InvalidOperationException("Email already exists.");

            CreateHash(dto.Password, out var hash, out var salt);
            var role = Enum.TryParse<UserRole>(dto.Role ?? "Guest", true, out var r) ? r : UserRole.Guest;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email.ToLower(),
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var sess = await CreateSessionAsync(user);
            return new SessionDto(sess.Token, user.UserId, user.FullName, user.Role.ToString());
        }

        public async Task<SessionDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower())
                       ?? throw new UnauthorizedAccessException("Invalid credentials.");
            if (!Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var sess = await CreateSessionAsync(user);
            return new SessionDto(sess.Token, user.UserId, user.FullName, user.Role.ToString());
        }

        public async Task<User?> ValidateTokenAsync(string token)
        {
            var s = await _db.UserSessions.Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token && x.IsActive && x.ExpiresAtUtc > DateTime.UtcNow);
            return s?.User;
        }

        public async Task LogoutAsync(string token)
        {
            var s = await _db.UserSessions.FirstOrDefaultAsync(x => x.Token == token);
            if (s != null) { s.IsActive = false; await _db.SaveChangesAsync(); }
        }

        private async Task<UserSession> CreateSessionAsync(User user)
        {
            var s = new UserSession
            {
                UserId = user.UserId,
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                IsActive = true
            };
            _db.UserSessions.Add(s);
            await _db.SaveChangesAsync();
            return s;
        }
    }
}

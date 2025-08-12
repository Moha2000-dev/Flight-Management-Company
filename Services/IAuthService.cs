using FlightApp.DTOs;
using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IAuthService
    {
        Task<SessionDto> RegisterAsync(RegisterDto dto);
        Task<SessionDto> LoginAsync(LoginDto dto);
        Task<User?> ValidateTokenAsync(string token);
        Task LogoutAsync(string token);
    }
}

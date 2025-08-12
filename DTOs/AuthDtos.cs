namespace FlightApp.DTOs
{
    public record RegisterDto(string FullName, string Email, string Password, string? Role = "Guest");
    public record LoginDto(string Email, string Password);
    public record SessionDto(string Token, int UserId, string FullName, string Role);
}

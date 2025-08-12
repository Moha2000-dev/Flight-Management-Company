using FlightApp.Models;

namespace FlightApp.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Passenger> GetOrCreatePassengerAsync(string fullName, string passportNo, string nationality, DateTime dob);
        Task<(int Capacity, HashSet<string> Taken)> GetSeatMapAsync(int flightId);
        Task<decimal> GetMinFareAsync(int flightId);
        Task<Booking> CreateBookingAsync(int passengerId, int flightId, IEnumerable<string> seatNumbers, decimal farePerSeat);
        Task<List<Booking>> GetByPassportAsync(string passportNo);
    }
}

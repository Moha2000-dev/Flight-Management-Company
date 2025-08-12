using FlightApp.Models;

namespace FlightApp.Services
{
    public interface IBookingService
    {
        Task<(bool ok, string msg, Booking? booking)> BookAsync(
            string token, int flightId, int seats,
            string passengerFullName, string passportNo, string nationality, DateTime dob,
            decimal? fareOverride = null);

        Task<List<Booking>> GetBookingsByPassportAsync(string passportNo);
    }
}

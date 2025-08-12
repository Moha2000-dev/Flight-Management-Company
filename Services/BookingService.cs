using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightApp.Models;
using FlightApp.Repositories;

namespace FlightApp.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;
        private readonly IAuthService _auth;

        public BookingService(IBookingRepository repo, IAuthService auth)
        {
            _repo = repo; _auth = auth;
        }

        public async Task<(bool ok, string msg, Booking? booking)> BookAsync(
            string token, int flightId, int seats,
            string passengerFullName, string passportNo, string nationality, DateTime dob,
            decimal? fareOverride = null)
        {
            var user = await _auth.ValidateTokenAsync(token);
            if (user is null) return (false, "Invalid/expired session.", null);
            if (seats <= 0) return (false, "Seats must be > 0.", null);

            var pax = await _repo.GetOrCreatePassengerAsync(passengerFullName, passportNo, nationality, dob);

            var (capacity, taken) = await _repo.GetSeatMapAsync(flightId);
            if (taken.Count + seats > capacity)
                return (false, $"Only {capacity - taken.Count} seat(s) left.", null);

            var seatList = new List<string>();
            for (int i = 0; i < seats; i++)
            {
                var seat = NextSeatLabel(taken, capacity);
                if (seat is null) return (false, "Seat allocation failed.", null);
                taken.Add(seat);
                seatList.Add(seat);
            }

            var fare = fareOverride ?? await _repo.GetMinFareAsync(flightId);
            if (fare <= 0) fare = 99m;

            var booking = await _repo.CreateBookingAsync(pax.PassengerId, flightId, seatList, fare);
            return (true, "Booked", booking);
        }

        public Task<List<Booking>> GetBookingsByPassportAsync(string passportNo) =>
            _repo.GetByPassportAsync(passportNo);

        private static string? NextSeatLabel(HashSet<string> taken, int capacity)
        {
            for (int i = 1; i <= capacity; i++)
            {
                var s = $"S{i:000}";
                if (!taken.Contains(s)) return s;
            }
            return null;
        }
    }
}

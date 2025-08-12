using FlightApp.DTOs;
using FlightApp.Services;
using System.Globalization;

namespace FlightApp.UI
{
    public class ConsoleUi
    {
        private readonly IAuthService _auth;
        private readonly IFlightService _flight;
        private readonly IBookingService _booking;
        private readonly IAdminService _admin;

        private string? _token;
        private string _role = "Guest";
        private string _name = "Guest";

        public ConsoleUi(IAuthService auth, IFlightService flight, IBookingService booking, IAdminService admin)
        {
            _auth = auth; _flight = flight; _booking = booking; _admin = admin;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=== Flight Management ===");
            while (true)
            {
                if (_token is null) { await AuthMenuAsync(); continue; }
                if (_role == "Admin") await AdminMenuAsync();
                else await GuestMenuAsync();
            }
        }

        private async Task AuthMenuAsync()
        {
            Console.WriteLine("\n[1] Login  [2] Register  [0] Exit");
            var c = Console.ReadLine();
            if (c == "0") Environment.Exit(0);

            Console.Write("Email: "); var email = Console.ReadLine()!.Trim().ToLower();
            Console.Write("Password: "); var pass = ReadHidden();

            try
            {
                if (c == "2")
                {
                    Console.Write("Full name: "); var name = Console.ReadLine()!;
                    Console.Write("Role (Admin/Agent/Guest) [Guest]: "); var role = Console.ReadLine();
                    var s = await _auth.RegisterAsync(new RegisterDto(name, email, pass, string.IsNullOrWhiteSpace(role) ? "Guest" : role));
                    (_token, _name, _role) = (s.Token, s.FullName, s.Role);
                }
                else
                {
                    var s = await _auth.LoginAsync(new LoginDto(email, pass));
                    (_token, _name, _role) = (s.Token, s.FullName, s.Role);
                }
                Console.WriteLine($"Welcome {_name}! Role: {_role}");
            }
            catch (Exception ex) { Console.WriteLine($"Auth failed: {ex.Message}"); }
        }

        private async Task GuestMenuAsync()
        {
            Console.WriteLine("\nGuest: [1] Search  [2] Book  [3] My bookings  [9] Logout  [0] Exit");
            var c = Console.ReadLine();
            if (c == "0") Environment.Exit(0);
            if (c == "9") { _token = null; return; }

            switch (c)
            {
                case "1": await DoSearchAsync(); break;
                case "2": await DoBookAsync(); break;
                case "3":
                    Console.Write("Passport: "); var p = Console.ReadLine()!;
                    var list = await _booking.GetBookingsByPassportAsync(p);
                    foreach (var b in list)
                    {
                        var f = b.Tickets.FirstOrDefault()?.Flight;
                        Console.WriteLine($"PNR {b.BookingRef} -> {f?.FlightNumber} {f?.Route?.OriginAirport?.IATA}->{f?.Route?.DestinationAirport?.IATA} Seats:{b.Tickets.Count}");
                    }
                    break;
            }
        }

        private async Task AdminMenuAsync()
        {
            Console.WriteLine("\nAdmin: [1] Add Airport  [2] Add Route  [3] Add Aircraft  [4] Add Flight  [5] Search  [9] Logout  [0] Exit");
            var c = Console.ReadLine();
            if (c == "0") Environment.Exit(0);
            if (c == "9") { _token = null; return; }

            try
            {
                switch (c)
                {
                    case "1":
                        Console.Write("IATA: "); var i = Console.ReadLine()!;
                        Console.Write("Name: "); var n = Console.ReadLine()!;
                        Console.Write("City: "); var city = Console.ReadLine()!;
                        Console.Write("Country: "); var ct = Console.ReadLine()!;
                        Console.Write("TimeZone [UTC]: "); var tz = Console.ReadLine(); if (string.IsNullOrWhiteSpace(tz)) tz = "UTC";
                        await _admin.AddAirportAsync(_token!, i, n, city, ct, tz);
                        Console.WriteLine("Airport added."); break;

                    case "2":
                        Console.Write("Origin IATA: "); var o = Console.ReadLine()!;
                        Console.Write("Dest IATA: "); var d = Console.ReadLine()!;
                        Console.Write("Distance km: "); var km = int.Parse(Console.ReadLine()!);
                        await _admin.AddRouteAsync(_token!, o, d, km);
                        Console.WriteLine("Route added."); break;

                    case "3":
                        Console.Write("Tail: "); var tail = Console.ReadLine()!;
                        Console.Write("Model: "); var model = Console.ReadLine()!;
                        Console.Write("Capacity: "); var cap = int.Parse(Console.ReadLine()!);
                        await _admin.AddAircraftAsync(_token!, tail, model, cap);
                        Console.WriteLine("Aircraft added."); break;

                    case "4":
                        Console.Write("Flight No: "); var fn = Console.ReadLine()!;
                        Console.Write("Origin IATA: "); var ro = Console.ReadLine()!;
                        Console.Write("Dest IATA: "); var rd = Console.ReadLine()!;
                        Console.Write("Departure (yyyy-MM-dd HH:mm UTC): "); var dep = ParseUtc(Console.ReadLine()!);
                        Console.Write("Arrival   (yyyy-MM-dd HH:mm UTC): "); var arr = ParseUtc(Console.ReadLine()!);
                        Console.Write("Tail: "); var ta = Console.ReadLine()!;
                        await _admin.AddFlightAsync(_token!, fn, ro, rd, dep, arr, ta);
                        Console.WriteLine("Flight added."); break;

                    case "5": await DoSearchAsync(); break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"Admin failed: {ex.Message}"); }
        }

        private async Task DoSearchAsync()
        {
            Console.Write("From (yyyy-MM-dd) or blank: "); var from = Console.ReadLine();
            Console.Write("To   (yyyy-MM-dd) or blank: "); var to = Console.ReadLine();
            Console.Write("Origin IATA or blank: "); var origin = Console.ReadLine();
            Console.Write("Dest   IATA or blank: "); var dest = Console.ReadLine();
            Console.Write("Min fare or blank: "); var min = Console.ReadLine();
            Console.Write("Max fare or blank: "); var max = Console.ReadLine();

            DateTime? fromUtc = string.IsNullOrWhiteSpace(from) ? null : DateTime.Parse(from!).ToUniversalTime();
            DateTime? toUtc = string.IsNullOrWhiteSpace(to) ? null : DateTime.Parse(to!).AddDays(1).AddSeconds(-1).ToUniversalTime();
            decimal? minFare = string.IsNullOrWhiteSpace(min) ? null : decimal.Parse(min!);
            decimal? maxFare = string.IsNullOrWhiteSpace(max) ? null : decimal.Parse(max!);

            var results = await _flight.SearchAsync(new FlightSearchRequest(fromUtc, toUtc, origin, dest, minFare, maxFare));
            if (!results.Any()) { Console.WriteLine("No flights."); return; }
            foreach (var r in results)
                Console.WriteLine($"{r.FlightId}: {r.FlightNumber} {r.OriginIata}->{r.DestIata} {r.DepartureUtc:u} FareFrom:{r.MinFare} SeatsSold:{r.SeatsSold}");
        }

        private async Task DoBookAsync()
        {
            Console.Write("FlightId: "); var fid = int.Parse(Console.ReadLine()!);
            Console.Write("Seats: "); var seats = int.Parse(Console.ReadLine()!);
            Console.Write("Passenger name: "); var name = Console.ReadLine()!;
            Console.Write("Passport: "); var pass = Console.ReadLine()!;
            Console.Write("Nationality: "); var nat = Console.ReadLine()!;
            Console.Write("DOB (yyyy-MM-dd): "); var dob = DateTime.Parse(Console.ReadLine()!);

            var (ok, msg, b) = await _booking.BookAsync(_token!, fid, seats, name, pass, nat, dob);
            Console.WriteLine(ok
                ? $"Booked: {b!.BookingRef} seats [{string.Join(",", b.Tickets.Select(t => t.SeatNumber))}] fare {b.Tickets.First().Fare}"
                : $"Booking failed: {msg}");
        }

        private static string ReadHidden()
        {
            var sb = new System.Text.StringBuilder();
            ConsoleKeyInfo k;
            while ((k = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (k.Key == ConsoleKey.Backspace && sb.Length > 0) { sb.Length--; continue; }
                if (!char.IsControl(k.KeyChar)) sb.Append(k.KeyChar);
            }
            Console.WriteLine();
            return sb.ToString();
        }
        private static DateTime ParseUtc(string txt) =>
            DateTime.SpecifyKind(DateTime.ParseExact(txt, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture), DateTimeKind.Utc);
    }
}

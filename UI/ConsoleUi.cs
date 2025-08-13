using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlightApp.DTOs;
using FlightApp.Services;

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
            Console.Title = "Flight Management Company";
            WriteBanner();

            while (true)
            {
                try
                {
                    if (_token is null) { await AuthMenuAsync(); continue; }
                    if (_role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) await AdminMenuAsync();
                    else await GuestMenuAsync();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        // ========= AUTH =========
        private async Task AuthMenuAsync()
        {
            Console.WriteLine("\n[ AUTH ]  1) Login   2) Register   0) Exit");
            Console.Write("> ");
            var c = Console.ReadLine();

            if (c == "0") Environment.Exit(0);

            Console.Write("Email: ");
            var email = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            Console.Write("Password: ");
            var pass = ReadHidden(masked: true);

            try
            {
                if (c == "2")
                {
                    Console.Write("Full name: "); var name = Console.ReadLine() ?? "";
                    Console.Write("Role (Admin/Agent/Guest) [Guest]: "); var role = Console.ReadLine();
                    var s = await _auth.RegisterAsync(new RegisterDto(name, email, pass,
                        string.IsNullOrWhiteSpace(role) ? "Guest" : role));
                    SetSession(s);
                }
                else
                {
                    var s = await _auth.LoginAsync(new LoginDto(email, pass));
                    SetSession(s);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Welcome {_name}! Role: {_role}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Auth failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        // ========= GUEST =========
        private async Task GuestMenuAsync()
        {
            Console.WriteLine($"\n[ GUEST ] ({_name})  1) Search  2) Book  3) My bookings  9) Logout  0) Exit");
            Console.Write("> ");
            var c = Console.ReadLine();

            if (c == "0") Environment.Exit(0);
            if (c == "9") { await SafeLogoutAsync(); return; }

            switch (c)
            {
                case "1": await DoSearchAsync(); break;
                case "2": await DoBookAsync(); break;
                case "3": await DoListBookingsAsync(); break;
                default: Console.WriteLine("Unknown option."); break;
            }
        }

        // ========= ADMIN =========
        private async Task AdminMenuAsync()
        {
            Console.WriteLine($"\n[ ADMIN ] ({_name})");
            Console.WriteLine(" 1) Add Airport");
            Console.WriteLine(" 2) Add Route");
            Console.WriteLine(" 3) Add Aircraft");
            Console.WriteLine(" 4) Add Flight");
            Console.WriteLine(" 5) Search Flights");
            Console.WriteLine(" 6) Add Maintenance");
            Console.WriteLine(" 7) Complete Maintenance");
            Console.WriteLine(" 8) Book (as admin)");
            Console.WriteLine(" 9) Logout");
            Console.WriteLine(" 0) Exit");
            Console.Write("> ");
            var c = Console.ReadLine();

            if (c == "0") Environment.Exit(0);
            if (c == "9") { await SafeLogoutAsync(); return; }

            try
            {
                switch (c)
                {
                    case "1":
                        var iata = Prompt("IATA").ToUpperInvariant();
                        var name = Prompt("Name");
                        var city = Prompt("City");
                        var country = Prompt("Country");
                        var tz = Prompt("TimeZone [UTC]", allowEmpty: true);
                        if (string.IsNullOrWhiteSpace(tz)) tz = "UTC";
                        await _admin.AddAirportAsync(_token!, iata, name, city, country, tz);
                        Success("Airport added.");
                        break;

                    case "2":
                        var o = Prompt("Origin IATA").ToUpperInvariant();
                        var d = Prompt("Dest IATA").ToUpperInvariant();
                        var km = ReadInt("Distance (km)");
                        var rid = await _admin.AddRouteAsync(_token!, o, d, km);
                        Success($"Route added. Id={rid}");
                        break;

                    case "3":
                        var tail = Prompt("Tail").ToUpperInvariant();
                        var model = Prompt("Model");
                        var cap = ReadInt("Capacity");
                        await _admin.AddAircraftAsync(_token!, tail, model, cap);
                        Success("Aircraft added.");
                        break;

                    case "4":
                        var fn = Prompt("Flight No").ToUpperInvariant();
                        var ro = Prompt("Origin IATA").ToUpperInvariant();
                        var rd = Prompt("Dest IATA").ToUpperInvariant();
                        var dep = ReadUtc("Departure (yyyy-MM-dd HH:mm UTC)");
                        var arr = ReadUtc("Arrival   (yyyy-MM-dd HH:mm UTC)");
                        var ta = Prompt("Tail").ToUpperInvariant();
                        var fid = await _admin.AddFlightAsync(_token!, fn, ro, rd, dep, arr, ta);
                        Success($"Flight added. Id={fid}");
                        break;

                    case "5": await DoSearchAsync(); break;

                    case "6":
                        var mtTail = Prompt("Tail").ToUpperInvariant();
                        var wt = Prompt("Work type (e.g., A-check)");
                        var notes = Prompt("Notes (optional)", allowEmpty: true);
                        var grounds = ReadYesNo("Grounds aircraft (y/n)? ");
                        var mid = await _admin.AddMaintenanceAsync(_token!, mtTail, wt, notes, grounds);
                        Success($"Maintenance created. Id={mid}");
                        break;

                    case "7":
                        var compId = ReadInt("Maintenance Id");
                        await _admin.CompleteMaintenanceAsync(_token!, compId);
                        Success("Marked as completed.");
                        break;

                    case "8":
                        await DoBookAsync();
                        break;

                    default:
                        Console.WriteLine("Unknown option.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Admin failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        // ========= ACTIONS =========
        private async Task DoSearchAsync()
        {
            var from = Prompt("From (yyyy-MM-dd) or blank", allowEmpty: true);
            var to = Prompt("To   (yyyy-MM-dd) or blank", allowEmpty: true);
            var origin = Prompt("Origin IATA or blank", allowEmpty: true);
            var dest = Prompt("Dest   IATA or blank", allowEmpty: true);
            var min = Prompt("Min fare or blank", allowEmpty: true);
            var max = Prompt("Max fare or blank", allowEmpty: true);

            DateTime? fromUtc = string.IsNullOrWhiteSpace(from) ? null : DateTime.Parse(from!).ToUniversalTime();
            DateTime? toUtc = string.IsNullOrWhiteSpace(to) ? null : DateTime.Parse(to!).AddDays(1).AddSeconds(-1).ToUniversalTime();
            decimal? minFare = string.IsNullOrWhiteSpace(min) ? null : decimal.Parse(min!, CultureInfo.InvariantCulture);
            decimal? maxFare = string.IsNullOrWhiteSpace(max) ? null : decimal.Parse(max!, CultureInfo.InvariantCulture);

            var results = await _flight.SearchAsync(new FlightSearchRequest(fromUtc, toUtc, origin, dest, minFare, maxFare));
            if (!results.Any()) { Info("No flights."); return; }

            Console.WriteLine("ID    Flight     Route   Departure(UTC)          FareFrom SeatsSold");
            Console.WriteLine("---------------------------------------------------------------------");
            foreach (var r in results)
                Console.WriteLine($"{r.FlightId,-5} {r.FlightNumber,-9} {r.OriginIata}->{r.DestIata,-5} {r.DepartureUtc:u} {r.MinFare,8} {r.SeatsSold,9}");
        }

        private async Task DoBookAsync()
        {
            var fid = ReadInt("FlightId");
            var seats = ReadInt("Seats");
            var name = Prompt("Passenger name");
            var pass = Prompt("Passport");
            var nat = Prompt("Nationality");
            var dob = DateTime.Parse(Prompt("DOB (yyyy-MM-dd)"), CultureInfo.InvariantCulture);

            var (ok, msg, b) = await _booking.BookAsync(_token!, fid, seats, name, pass, nat, dob);
            if (ok)
                Success($"Booked: {b!.BookingRef} seats [{string.Join(",", b.Tickets.Select(t => t.SeatNumber))}] fare {b.Tickets.First().Fare}");
            else
                Warn($"Booking failed: {msg}");
        }

        private async Task DoListBookingsAsync()
        {
            var p = Prompt("Passport");
            var list = await _booking.GetBookingsByPassportAsync(p);
            if (!list.Any()) { Info("No bookings."); return; }

            foreach (var b in list)
            {
                var f = b.Tickets.FirstOrDefault()?.Flight;
                Console.WriteLine($"PNR {b.BookingRef} -> {f?.FlightNumber} {f?.Route?.OriginAirport?.IATA}->{f?.Route?.DestinationAirport?.IATA} Seats:{b.Tickets.Count}");
            }
        }

        // ========= HELPERS =========
        private void SetSession(SessionDto s) { _token = s.Token; _name = s.FullName; _role = s.Role; }

        private async Task SafeLogoutAsync()
        {
            try { if (_token != null) await _auth.LogoutAsync(_token); }
            catch { /* ignore */ }
            _token = null; _role = "Guest"; _name = "Guest";
            Info("Logged out.");
        }

        private static void WriteBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================");
            Console.WriteLine("          Flight Management Company            ");
            Console.WriteLine("===============================================");
            Console.ResetColor();
        }

        private static string Prompt(string label, bool allowEmpty = false)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                var s = Console.ReadLine() ?? "";
                if (allowEmpty || !string.IsNullOrWhiteSpace(s)) return s.Trim();
                Console.WriteLine("Value required.");
            }
        }

        private static int ReadInt(string label)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                if (int.TryParse(Console.ReadLine(), out var v)) return v;
                Console.WriteLine("Enter a valid integer.");
            }
        }

        private static DateTime ReadUtc(string label)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                var s = Console.ReadLine() ?? "";
                if (DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture,
                                           DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                           out var dt))
                {
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
                Console.WriteLine("Format must be yyyy-MM-dd HH:mm (UTC).");
            }
        }

        private static bool ReadYesNo(string label)
        {
            while (true)
            {
                Console.Write(label);
                var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
                if (s.StartsWith("y")) return true;
                if (s.StartsWith("n")) return false;
                Console.WriteLine("Please enter y or n.");
            }
        }

        private static string ReadHidden(bool masked = true)
        {
            if (!masked || Console.IsInputRedirected) return Console.ReadLine() ?? string.Empty;

            var sb = new StringBuilder();
            while (true)
            {
                var k = Console.ReadKey(intercept: true);
                if (k.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
                if (k.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
                    continue;
                }
                if (!char.IsControl(k.KeyChar)) { sb.Append(k.KeyChar); Console.Write('*'); }
            }
            return sb.ToString();
        }

        private static void Success(string m) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(m); Console.ResetColor(); }
        private static void Warn(string m) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(m); Console.ResetColor(); }
        private static void Info(string m) { Console.ForegroundColor = ConsoleColor.DarkCyan; Console.WriteLine(m); Console.ResetColor(); }
    }
}

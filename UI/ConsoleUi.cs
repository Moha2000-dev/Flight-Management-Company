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
                    if (_role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        await AdminMenuAsync();
                    else
                        await GuestMenuAsync();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        // ===================== AUTH (top level) =====================
        private async Task AuthMenuAsync()
        {
            Console.WriteLine("\n[ AUTH ]  1) Login   2) Register   T) Tasks (read-only)   0) Exit");
            Console.Write("> ");
            var c = (Console.ReadLine() ?? "").Trim();

            if (c.Equals("0", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);
            if (c.Equals("T", StringComparison.OrdinalIgnoreCase)) { await TasksMenuAsync(); return; }

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
                    var s = await _auth.RegisterAsync(new RegisterDto(
                        name, email, pass, string.IsNullOrWhiteSpace(role) ? "Guest" : role));
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

        // ===================== GUEST =====================
        private async Task GuestMenuAsync()
        {
            Console.WriteLine($"\n[ GUEST ] ({_name})  1) Search  2) Book  3) My bookings  T) Tasks  9) Logout  0) Exit");
            Console.Write("> ");
            var c = (Console.ReadLine() ?? "").Trim();

            if (c.Equals("0", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);
            if (c.Equals("9", StringComparison.OrdinalIgnoreCase)) { await SafeLogoutAsync(); return; }
            if (c.Equals("T", StringComparison.OrdinalIgnoreCase)) { await TasksMenuAsync(); return; }

            switch (c)
            {
                case "1": await DoSearchAsync(); break;
                case "2": await DoBookAsync(); break;
                case "3": await DoListBookingsAsync(); break;
                default: Console.WriteLine("Unknown option."); break;
            }
        }

        // ===================== ADMIN =====================
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
            Console.WriteLine(" T) Tasks");
            Console.WriteLine(" 9) Logout");
            Console.WriteLine(" 0) Exit");
            Console.Write("> ");
            var c = (Console.ReadLine() ?? "").Trim();

            if (c.Equals("0", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);
            if (c.Equals("9", StringComparison.OrdinalIgnoreCase)) { await SafeLogoutAsync(); return; }
            if (c.Equals("T", StringComparison.OrdinalIgnoreCase)) { await TasksMenuAsync(); return; }

            try
            {
                switch (c)
                {
                    case "1":
                        {
                            var iata = Prompt("IATA").ToUpperInvariant();
                            var name = Prompt("Name");
                            var city = Prompt("City");
                            var country = Prompt("Country");
                            var tz = Prompt("TimeZone [UTC]", allowEmpty: true);
                            if (string.IsNullOrWhiteSpace(tz)) tz = "UTC";
                            await _admin.AddAirportAsync(_token!, iata, name, city, country, tz);
                            Success("Airport added.");
                            break;
                        }
                    case "2":
                        {
                            var o = Prompt("Origin IATA").ToUpperInvariant();
                            var d = Prompt("Dest IATA").ToUpperInvariant();
                            var km = ReadInt("Distance (km)");
                            var rid = await _admin.AddRouteAsync(_token!, o, d, km);
                            Success($"Route added. Id={rid}");
                            break;
                        }
                    case "3":
                        {
                            var tail = Prompt("Tail").ToUpperInvariant();
                            var model = Prompt("Model");
                            var cap = ReadInt("Capacity");
                            await _admin.AddAircraftAsync(_token!, tail, model, cap);
                            Success("Aircraft added.");
                            break;
                        }
                    case "4":
                        {
                            var fn = Prompt("Flight No").ToUpperInvariant();
                            var ro = Prompt("Origin IATA").ToUpperInvariant();
                            var rd = Prompt("Dest IATA").ToUpperInvariant();
                            var dep = ReadUtc("Departure (yyyy-MM-dd HH:mm UTC)");
                            var arr = ReadUtc("Arrival   (yyyy-MM-dd HH:mm UTC)");
                            var ta = Prompt("Tail").ToUpperInvariant();
                            var fid = await _admin.AddFlightAsync(_token!, fn, ro, rd, dep, arr, ta);
                            Success($"Flight added. Id={fid}");
                            break;
                        }
                    case "5":
                        await DoSearchAsync(); break;

                    case "6":
                        {
                            var mtTail = Prompt("Tail").ToUpperInvariant();
                            var wt = Prompt("Work type (e.g., A-check)");
                            var notes = Prompt("Notes (optional)", allowEmpty: true);
                            var grounds = ReadYesNo("Grounds aircraft (y/n)? ");
                            var mid = await _admin.AddMaintenanceAsync(_token!, mtTail, wt, notes, grounds);
                            Success($"Maintenance created. Id={mid}");
                            break;
                        }
                    case "7":
                        {
                            var compId = ReadInt("Maintenance Id");
                            await _admin.CompleteMaintenanceAsync(_token!, compId);
                            Success("Marked as completed.");
                            break;
                        }
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

        // ===================== TASKS =====================
        private async Task TasksMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("\n[ TASKS ]");
                Console.WriteLine(" 1) Daily flight manifest");
                Console.WriteLine(" 2) Top routes by revenue (last 7 days)");
                Console.WriteLine(" 3) On-time performance (±10 min) by route (±7 days)");
                Console.WriteLine(" 4) Seat occupancy heatmap (>=80% next/prev 7 days)");
                Console.WriteLine(" 5) Find available seats (by FlightId)");
                Console.WriteLine(" 6) Crew scheduling conflicts (±7 days)");
                Console.WriteLine(" 7) Passengers with connections (max layover hours)");
                Console.WriteLine(" 8) Frequent fliers (Top 10 by flights)");
                Console.WriteLine(" 9) Maintenance alerts (dist>=10000km or last>30 days)");
                Console.WriteLine("10) Baggage overweight alerts (> 30kg per ticket)");
                Console.WriteLine("11) Flights paging example (page 1 size 10)");
                Console.WriteLine("12) Conversion ops (ToDictionary / ToArray)");
                Console.WriteLine("13) Running daily revenue (last 30 days)");
                Console.WriteLine("14) Forecast next week (tickets/day)");
                Console.WriteLine(" 0) Back");

                var pick = (Console.ReadLine() ?? "").Trim();
                if (pick == "0") break;

                try
                {
                    switch (pick)
                    {
                        case "1":
                            {
                                Console.Write("Date (yyyy-MM-dd) blank=today: ");
                                var txt = Console.ReadLine();
                                var day = string.IsNullOrWhiteSpace(txt)
                                    ? DateTime.UtcNow
                                    : DateTime.SpecifyKind(DateTime.Parse(txt), DateTimeKind.Utc);

                                var rows = await _flight.GetDailyManifestAsync(day);
                                if (!rows.Any()) { Console.WriteLine("No flights for that date."); break; }
                                foreach (var x in rows)
                                    Console.WriteLine(
      $"{x.FlightNumber} {x.OriginIata}->{x.DestIata} {x.DepartureUtc:u} pax:{x.TicketsSold}");

                                break;
                            }
                        case "2":
                            {
                                var to = DateTime.UtcNow; var from = to.AddDays(-7);
                                var top = await _flight.GetTopRoutesByRevenueAsync(from, to, 10);
                                if (!top.Any()) { Console.WriteLine("No data."); break; }
                                foreach (var r in top)
                                    Console.WriteLine($"{r.OriginIata}->{r.DestIata} Flights:{r.Flights} Tickets:{r.Tickets} Revenue:{r.Revenue:F2}");
                                break;
                            }
                        case "3":
                            {
                                var from = DateTime.UtcNow.AddDays(-7); var to = DateTime.UtcNow.AddDays(7);
                                var perf = await _flight.OnTimePerformanceAsync(from, to, 10, byRoute: true);
                                foreach (var r in perf.Take(20))
                                    Console.WriteLine($"{r.Key} OnTime:{r.PctOnTime}% ({r.OnTime}/{r.Flights})");
                                break;
                            }
                        case "4":
                            {
                                var from = DateTime.UtcNow.AddDays(-7); var to = DateTime.UtcNow.AddDays(7);
                                var rows = await _flight.GetHighOccupancyAsync(from, to, 80);
                                if (!rows.Any()) { Console.WriteLine("No high-occupancy flights in window."); break; }
                                foreach (var r in rows)
                                    Console.WriteLine($"{r.FlightNumber} {r.DepartureUtc:u} {r.Percent}% ({r.SeatsSold}/{r.Capacity})");
                                break;
                            }
                        case "5":
                            {
                                Console.Write("FlightId: ");
                                if (!int.TryParse(Console.ReadLine(), out var fid)) { Console.WriteLine("Invalid id."); break; }
                                var a = await _flight.GetAvailableSeatsAsync(fid); // summary DTO
                                if (a is null) { Console.WriteLine("Flight not found."); break; }
                                Console.WriteLine($"{a.FlightNumber} Capacity:{a.Capacity} Sold:{a.SeatsSold} Available:{a.SeatsAvailable}");
                                break;
                            }
                        case "6":
                            {
                                var from = DateTime.UtcNow.AddDays(-7); var to = DateTime.UtcNow.AddDays(7);
                                var rows = await _flight.CrewConflictsAsync(from, to);
                                if (!rows.Any()) { Console.WriteLine("No conflicts."); break; }
                                foreach (var x in rows.Take(30))
                                    Console.WriteLine($"{x.CrewName} conflict between flights {x.FlightAId} and {x.FlightBId}");
                                break;
                            }
                        case "7":
                            {
                                var h = ReadInt("Max layover hours [6] (press Enter for 6)");
                                if (h == 0) h = 6;
                                var rows = await _flight.GetPassengersWithConnectionsAsync(h);
                                if (!rows.Any()) { Console.WriteLine("None found."); break; }
                                foreach (var x in rows.Take(30))
                                    Console.WriteLine($"{x.PassengerName} {x.FlightA}->{x.FlightB} {x.OriginIata}->{x.ViaIata}->{x.DestIata} layover:{x.LayoverMinutes}m");
                                break;
                            }
                        case "8":
                            {
                                var rows = await _flight.FrequentFliersAsync(10, byFlights: true);
                                foreach (var x in rows)
                                    Console.WriteLine($"{x.FullName} Flights:{x.Flights} Dist:{x.DistanceKm}km");
                                break;
                            }
                        case "9":
                            {
                                var rows = await _flight.MaintenanceAlertsAsync(distanceThresholdKm: 10000, olderThanDays: 30);
                                foreach (var a in rows.Take(20))
                                    Console.WriteLine($"{a.TailNumber} Flights:{a.Flights} Dist:{a.DistanceKm}km Last:{a.LastCompletedUtc:u} Needs:{a.NeedsAttention}");
                                break;
                            }
                        case "10":
                            {
                                var rows = await _flight.BaggageOverweightAlertsAsync(30m);
                                if (!rows.Any()) { Console.WriteLine("None."); break; }
                                foreach (var b in rows.Take(20))
                                    Console.WriteLine($"{b.BookingRef} {b.PassengerName} {b.FlightNumber} Total:{b.TotalWeightKg}kg");
                                break;
                            }
                        case "11":
                            {
                                var page = await _flight.FlightsPageAsync(page: 1, pageSize: 10, fromUtc: null, toUtc: null);
                                Console.WriteLine($"Page {page.Page}/{Math.Ceiling(page.Total / (double)page.PageSize)}  Total:{page.Total}");
                                foreach (var r in page.Rows)
                                    Console.WriteLine($"{r.FlightId} {r.FlightNumber} {r.OriginIata}->{r.DestIata} {r.DepartureUtc:u}");
                                break;
                            }
                        case "12":
                            {
                                var dict = await _flight.FlightsToDictionaryAsync();
                                Console.WriteLine($"Dictionary count: {dict.Count} (keys=FlightNumber)");

                                var to = DateTime.UtcNow; var from = to.AddDays(-7);
                                var arr = await _flight.TopRouteCodesArrayAsync(10, from, to);
                                Console.WriteLine($"Top routes last 7d: {string.Join(", ", arr)}");
                                break;
                            }
                        case "13":
                            {
                                var rows = await _flight.DailyRevenueRunningAsync(30);
                                foreach (var r in rows)
                                    Console.WriteLine($"{r.DayUtc:yyyy-MM-dd} Rev:{r.Revenue:F2} Running:{r.RunningTotal:F2}");
                                break;
                            }
                        case "14":
                            {
                                var rows = await _flight.ForecastNextWeekAsync();
                                foreach (var r in rows)
                                    Console.WriteLine($"{r.DayUtc:yyyy-MM-dd} expected tickets: {r.ExpectedTickets}");
                                break;
                            }
                        default:
                            Console.WriteLine("Pick 0..14");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
        }

        // ===================== SEARCH / BOOK / LIST =====================
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
            var flightId = await PromptFlightIdFromListAsync();
            if (flightId is null) return;

            var seats = ReadInt("Seats");
            var name = Prompt("Passenger name");
            var pass = Prompt("Passport");
            var nat = Prompt("Nationality");
            var dob = DateTime.Parse(Prompt("DOB (yyyy-MM-dd)"), CultureInfo.InvariantCulture);

            try
            {
                var available = await _flight.GetAvailableSeatsAsync(flightId.Value);
                if (available is null) { Console.WriteLine("Flight not found."); return; }

                if (available.SeatsAvailable < seats)
                {
                    Console.WriteLine($"Only {available.SeatsAvailable} seats left on FlightId {flightId}. Aborting.");
                    return;
                }

                var (ok, msg, b) = await _booking.BookAsync(_token!, flightId.Value, seats, name, pass, nat, dob);

                if (ok)
                    Success($"Booked: {b!.BookingRef} seats [{string.Join(",", b.Tickets.Select(t => t.SeatNumber))}] fare {b.Tickets.First().Fare}");
                else
                    Warn($"Booking failed: {msg}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + FullMessage(ex));
                Console.ResetColor();
            }
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

        // ===================== HELPERS =====================
        private void SetSession(SessionDto s) { _token = s.Token; _name = s.FullName; _role = s.Role; }

        private async Task SafeLogoutAsync()
        {
            try { if (_token != null) await _auth.LogoutAsync(_token); }
            catch { /* ignore */ }
            _token = null; _role = "Guest"; _name = "Guest";
            Info("Logged out.");
        }

        private async Task<int?> PromptFlightIdFromListAsync()
        {
            Console.Write("Origin IATA (blank=any): ");
            var origin = Console.ReadLine();
            Console.Write("Dest IATA (blank=any): ");
            var dest = Console.ReadLine();

            var results = await _flight.SearchAsync(new FlightSearchRequest(
                DateTime.UtcNow, DateTime.UtcNow.AddDays(7), origin, dest, null, null));

            if (!results.Any())
            {
                Console.WriteLine("No flights found for the filter.");
                return null;
            }

            var top = results.OrderBy(r => r.DepartureUtc).Take(10).ToList();
            Console.WriteLine("\nChoose a flight:");
            for (int i = 0; i < top.Count; i++)
            {
                var availDto = await _flight.GetAvailableSeatsAsync(top[i].FlightId);
                int avail = availDto?.SeatsAvailable ?? 0;
                Console.WriteLine($"{i + 1}) Id={top[i].FlightId} {top[i].FlightNumber} {top[i].OriginIata}->{top[i].DestIata}  {top[i].DepartureUtc:u}  Avail:{avail}");
            }

            Console.Write("Select number (1..10) or type a FlightId directly: ");
            var s = Console.ReadLine();

            if (int.TryParse(s, out var pick))
            {
                if (pick >= 1 && pick <= top.Count) return top[pick - 1].FlightId; // pick from list
                return pick; // typed a raw FlightId
            }

            Console.WriteLine("Invalid input.");
            return null;
        }

        private static string FullMessage(Exception ex)
        {
            var sb = new StringBuilder(ex.Message);
            var e = ex.InnerException;
            while (e != null)
            {
                sb.Append(" -> ");
                sb.Append(e.Message);
                e = e.InnerException;
            }
            return sb.ToString();
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
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) return 0; // treat blank as 0 (for defaults)
                if (int.TryParse(s, out var v)) return v;
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

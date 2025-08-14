using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlightApp.Data;
using FlightApp.Models;

namespace FlightApp.SeedData
{
    public static class DemoData
    {
        private static readonly Random _rand = new Random();

        /// Pushes ArrivalUtc forward for a random set of flights to simulate delays.
        /// DOES NOT introduce a new enum value. Keeps statuses valid (Scheduled/Departed/Landed/Canceled).
        public static async Task DelaysAsync(FlightDbContext db, int count, int minDelayMin, int maxDelayMin)
        {
            var now = DateTime.UtcNow;

            var flights = await db.Flights
                .Where(f =>
                    f.Status != FlightStatus.Canceled &&
                    f.DepartureUtc >= now.AddDays(-10) &&
                    f.DepartureUtc <= now.AddDays(10))
                .OrderBy(_ => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            foreach (var f in flights)
            {
                var delay = TimeSpan.FromMinutes(_rand.Next(minDelayMin, maxDelayMin + 1));

                // simulate a late arrival
                f.ArrivalUtc = f.ArrivalUtc.Add(delay);

                // keep status within existing enum values (optional, just to look coherent)
                if (f.Status == FlightStatus.Scheduled && f.DepartureUtc <= now)
                    f.Status = FlightStatus.Departed;

                if (f.Status == FlightStatus.Departed && f.ArrivalUtc <= now)
                    f.Status = FlightStatus.Landed;
            }

            await db.SaveChangesAsync();
        }
    }
}

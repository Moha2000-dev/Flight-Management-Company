using Microsoft.EntityFrameworkCore;
using FlightApp.Models;

namespace FlightApp.Data
{
    public class FlightDbContext : DbContext
    {
        // ---- Constructors ----
        public FlightDbContext() { } // design-time / no-DI fallback
        public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options) { }

        // ---- Fallback config so Tools can create the context ----
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True");
            }
        }

        // DbSets


        public DbSet<User> Users => Set<User>();
        public DbSet<UserSession> UserSessions => Set<UserSession>();
        public DbSet<Airport> Airports => Set<Airport>();
        public DbSet<Route> Routes => Set<Route>();
        public DbSet<Aircraft> Aircraft => Set<Aircraft>();
        public DbSet<Flight> Flights => Set<Flight>();
        public DbSet<CrewMember> CrewMembers => Set<CrewMember>();
        public DbSet<FlightCrew> FlightCrews => Set<FlightCrew>();
        public DbSet<Passenger> Passengers => Set<Passenger>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Baggage> Baggage => Set<Baggage>();
        public DbSet<AircraftMaintenance> Maintenances => Set<AircraftMaintenance>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // -------- Extra-safe unique indexes (mirror of attributes) ----------
            b.Entity<User>().HasIndex(u => u.Email).IsUnique();
            b.Entity<UserSession>().HasIndex(s => s.Token).IsUnique();
            b.Entity<Airport>().HasIndex(a => a.IATA).IsUnique();
            b.Entity<Aircraft>().HasIndex(a => a.TailNumber).IsUnique();
            b.Entity<Passenger>().HasIndex(p => p.PassportNo).IsUnique();
            b.Entity<Booking>().HasIndex(k => k.BookingRef).IsUnique();

            // -------- Airport <-> Route (two FKs to same table, restrict delete) ----------
            b.Entity<Route>()
             .HasOne(r => r.OriginAirport)
             .WithMany(a => a.OriginRoutes)
             .HasForeignKey(r => r.OriginAirportId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Route>()
             .HasOne(r => r.DestinationAirport)
             .WithMany(a => a.DestRoutes)
             .HasForeignKey(r => r.DestinationAirportId)
             .OnDelete(DeleteBehavior.Restrict);

            // -------- Flight unique (FlightNumber + DepartureUtc) ----------
            b.Entity<Flight>()
             .HasIndex(f => new { f.FlightNumber, f.DepartureUtc })
             .IsUnique();

            // -------- FlightCrew composite PK (many-to-many Flight↔CrewMember) ----------
            b.Entity<FlightCrew>()
             .HasKey(fc => new { fc.FlightId, fc.CrewId });

            b.Entity<FlightCrew>()
             .HasOne(fc => fc.Flight)
             .WithMany(f => f.FlightCrews)
             .HasForeignKey(fc => fc.FlightId);

            b.Entity<FlightCrew>()
             .HasOne(fc => fc.Crew)
             .WithMany(c => c.FlightCrews)
             .HasForeignKey(fc => fc.CrewId);

            // -------- Ticket unique seat per Flight ----------
            b.Entity<Ticket>()
             .HasIndex(t => new { t.FlightId, t.SeatNumber })
             .IsUnique();

            // -------- Cascade rules ----------
            b.Entity<Ticket>()
             .HasOne(t => t.Booking)
             .WithMany(bk => bk.Tickets)
             .HasForeignKey(t => t.BookingId)
             .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Baggage>()
             .HasOne(bg => bg.Ticket)
             .WithMany(t => t.Baggage)
             .HasForeignKey(bg => bg.TicketId)
             .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(b);
        }
    }
}

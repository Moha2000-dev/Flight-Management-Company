// FileContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using FlightApp.Data;

public class FileContext : IDesignTimeDbContextFactory<FlightDbContext>
{
    public FlightDbContext CreateDbContext(string[] args)
    {
        var cs = @"Server=(localdb)\MSSQLLocalDB;Database=FlightDB;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<FlightDbContext>()
            .UseSqlServer(cs)
            .Options;
        return new FlightDbContext(options);
    }
}

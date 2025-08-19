// Repositories/DapperReadRepository.cs
using Dapper;
using FlightApp.DTOs;
using System.Data;

public class DapperReadRepository
{
    private readonly Func<IDbConnection> _connFactory;
    public DapperReadRepository(Func<IDbConnection> connFactory) => _connFactory = connFactory;

    public async Task<List<RouteRevenueDto>> TopRoutesAsync(DateTime from, DateTime to, int top)
    {
        const string sql = @"
            SELECT oa.IATA OriginIata, da.IATA DestIata,
                   COUNT(DISTINCT t.FlightId) Flights,
                   COUNT(*) Tickets,
                   SUM(t.Fare) Revenue
            FROM Tickets t
            JOIN Flights f ON f.FlightId = t.FlightId
            JOIN Routes  r ON r.RouteId  = f.RouteId
            JOIN Airports oa ON oa.AirportId = r.OriginAirportId
            JOIN Airports da ON da.AirportId = r.DestinationAirportId
            WHERE f.DepartureUtc BETWEEN @from AND @to
            GROUP BY oa.IATA, da.IATA
            ORDER BY Revenue DESC
            OFFSET 0 ROWS FETCH NEXT @top ROWS ONLY;";

        using var conn = _connFactory();
        var rows = await conn.QueryAsync<RouteRevenueDto>(sql, new { from, to, top });
        return rows.ToList();
    }
}

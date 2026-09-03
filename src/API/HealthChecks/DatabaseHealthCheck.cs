using HotelPOS.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotelPOS.Api.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly HotelDbContext _context;

        public DatabaseHealthCheck(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database connection is healthy.")
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
    }
}

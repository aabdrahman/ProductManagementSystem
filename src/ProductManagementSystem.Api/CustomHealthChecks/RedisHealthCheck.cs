using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ProductManagementSystem.Api.CustomHealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbConnection = _connectionMultiplexer.GetDatabase();

            var pingResult = await dbConnection.PingAsync();

            var isHealthy = pingResult.TotalMilliseconds < 100;
            if (isHealthy)
            {
                return HealthCheckResult.Healthy($"Redis is healthy. Returns after {pingResult.TotalMilliseconds} ms");
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Redis is unhealthy. Returns after {pingResult.TotalMilliseconds} ms");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unhealthy", ex);
        }
    }
}

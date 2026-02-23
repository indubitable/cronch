using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace cronch.Services;

public class CronchHealthCheck(JobSchedulingService _schedulingService, ExecutionPersistenceService _executionPersistenceService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!await _schedulingService.IsRunningAsync())
        {
            return HealthCheckResult.Unhealthy("Scheduling service is not running");
        }

        try
        {
            if (!_executionPersistenceService.CheckDatabaseConnectivity())
            {
                return HealthCheckResult.Unhealthy("Database connectivity check failed");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed", ex);
        }

        return HealthCheckResult.Healthy();
    }
}

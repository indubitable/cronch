using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace cronch.Services;

public class CronchHealthCheck(JobSchedulingService _schedulingService, ExecutionPersistenceService _executionPersistenceService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_schedulingService.IsRunning)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Scheduling service is not running"));
        }

        try
        {
            if (!_executionPersistenceService.CheckDatabaseConnectivity())
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Database connectivity check failed"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Database connectivity check failed", ex));
        }

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}

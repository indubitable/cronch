
using cronch.Models;

namespace cronch.Services;

public class CleanupService(ILogger<CleanupService> _logger, JobConfigService _jobConfigService, SettingsService _settingsService, IServiceProvider _serviceProvider)
{
    private Thread? _runThread;

    public virtual void Initialize()
    {
        if (_runThread != null) return;

        _runThread = new Thread(RunCleanupThread)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal,
        };
        _runThread.Start();
    }

    private void RunCleanupThread()
    {
        // Only do this on startup:
        try
        {
            CleanUpExecutionsForDeletedJobs();
        }
        catch { }
        try
        {
            CleanUpOrphanedExecutionFiles();
        }
        catch { }

        while (true)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

                CleanUpExecutionsByCount(executionPersistenceService);
                CleanUpExecutionsByAge(executionPersistenceService);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception occurred during scheduled cleanup");
            }

            Thread.Sleep(TimeSpan.FromMinutes(20));
        }
    }

    internal void CleanUpExecutionsByAge(ExecutionPersistenceService executionPersistenceService)
    {
        var maxAgeDays = _settingsService.LoadSettings().DeleteHistoricalRunsAfterDays;
        if (!maxAgeDays.HasValue) return;

        var oldExecutions = executionPersistenceService.GetExecutionsOlderThan(DateTimeOffset.UtcNow.AddDays(-1 * maxAgeDays.Value));
        DeleteExecutions(executionPersistenceService, oldExecutions);
    }

    internal void CleanUpExecutionsByCount(ExecutionPersistenceService executionPersistenceService)
    {
        var maxCount = _settingsService.LoadSettings().DeleteHistoricalRunsAfterCount;
        if (!maxCount.HasValue) return;

        var allJobIds = _jobConfigService.GetAllJobs().Select(j => j.Id);
        foreach (var jobId in allJobIds)
        {
            var oldExecutions = executionPersistenceService.GetOldestExecutionsAfterCount(jobId, maxCount.Value);
            DeleteExecutions(executionPersistenceService, oldExecutions);
        }
    }

    private void DeleteExecutions(ExecutionPersistenceService executionPersistenceService, IEnumerable<ExecutionModel> executions)
    {
        foreach (var exec in executions)
        {
            try
            {
                executionPersistenceService.DeleteExecution(exec);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to clean up execution {ExecId} for job '{Name}' ({JobId})", exec.Id, exec.JobName, exec.JobId);
            }
        }
    }

    private void CleanUpExecutionsForDeletedJobs()
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        var allConfiguredJobIds = _jobConfigService.GetAllJobs().Select(j => j.Id);
        executionPersistenceService.CleanUpExecutionFilesForDeletedJobs(allConfiguredJobIds);
    }

    private void CleanUpOrphanedExecutionFiles()
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        var allConfiguredJobIds = _jobConfigService.GetAllJobs().Select(j => j.Id);
        foreach (var id in allConfiguredJobIds)
        {
            executionPersistenceService.CleanUpOrphanedExecutionFiles(id);
        }
    }
}

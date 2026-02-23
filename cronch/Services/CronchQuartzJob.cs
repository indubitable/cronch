using cronch.Models;
using Quartz;

namespace cronch.Services;

/// <summary>
/// Quartz IJob implementation that acts as a thin fire-and-forget dispatcher.
/// It resolves the job from config and delegates to <see cref="JobExecutionService"/>.
/// The Quartz thread pool thread is released immediately after dispatching.
/// </summary>
public class CronchQuartzJob(ILogger<CronchQuartzJob> _logger, JobConfigService _jobConfigService, JobExecutionService _jobExecutionService) : IJob
{
    public const string JobIdKey = "CronchJobId";

    public Task Execute(IJobExecutionContext context)
    {
        var jobIdString = context.MergedJobDataMap.GetString(JobIdKey);
        if (!Guid.TryParse(jobIdString, out var jobId))
        {
            _logger.LogError("CronchQuartzJob executed without a valid {Key} in JobDataMap", JobIdKey);
            return Task.CompletedTask;
        }

        var jobModel = _jobConfigService.GetJob(jobId);
        if (jobModel == null)
        {
            _logger.LogWarning("CronchQuartzJob: job {Id} not found in configuration, skipping execution", jobId);
            return Task.CompletedTask;
        }

        if (!jobModel.Enabled)
        {
            _logger.LogDebug("CronchQuartzJob: job '{Name}' ({Id}) is disabled, skipping execution", jobModel.Name, jobId);
            return Task.CompletedTask;
        }

        _logger.LogDebug("CronchQuartzJob: dispatching job '{Name}' ({Id})", jobModel.Name, jobId);
        _jobExecutionService.ExecuteJob(jobModel, ExecutionReason.Scheduled);

        return Task.CompletedTask;
    }
}

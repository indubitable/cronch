using cronch.Models;
using Quartz;
using Quartz.Impl.Matchers;

namespace cronch.Services;

public class JobSchedulingService(ILogger<JobSchedulingService> _logger, ISchedulerFactory _schedulerFactory)
{
    private const string TriggerGroupName = "cronch";
    private const string JobGroupName = "cronch";

    public virtual async Task<bool> IsRunningAsync()
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            return scheduler.IsStarted && !scheduler.IsShutdown;
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task RefreshSchedulesAsync(IEnumerable<JobModel> allJobs)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var enabledJobs = allJobs.Where(j => j.Enabled && !string.IsNullOrWhiteSpace(j.CronSchedule)).ToList();

        // Get all currently scheduled trigger keys in our group
        var existingTriggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(TriggerGroupName));

        var desiredTriggerKeys = new HashSet<string>(enabledJobs.Select(j => j.Id.ToString()));

        // Remove triggers for jobs that are no longer enabled or have been deleted
        foreach (var triggerKey in existingTriggerKeys)
        {
            if (!desiredTriggerKeys.Contains(triggerKey.Name))
            {
                await scheduler.UnscheduleJob(triggerKey);
                var jobKey = new JobKey(triggerKey.Name, JobGroupName);
                await scheduler.DeleteJob(jobKey);
                _logger.LogDebug("Unscheduled job {Id}", triggerKey.Name);
            }
        }

        // Add or update triggers for enabled jobs
        foreach (var job in enabledJobs)
        {
            try
            {
                await ScheduleOrUpdateJobAsync(scheduler, job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to schedule job '{Name}' ({Id})", job.Name, job.Id);
            }
        }
    }

    public virtual async Task<DateTimeOffset?> GetNextExecutionAsync(Guid jobId)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var triggerKey = new TriggerKey(jobId.ToString(), TriggerGroupName);
            var trigger = await scheduler.GetTrigger(triggerKey);
            return trigger?.GetNextFireTimeUtc();
        }
        catch
        {
            return null;
        }
    }

    private async Task ScheduleOrUpdateJobAsync(IScheduler scheduler, JobModel job)
    {
        var jobKey = new JobKey(job.Id.ToString(), JobGroupName);
        var triggerKey = new TriggerKey(job.Id.ToString(), TriggerGroupName);

        var cronExpression = job.CronSchedule!;

        // Validate the cron expression
        if (!CronExpression.IsValidExpression(cronExpression))
        {
            _logger.LogError("Invalid cron expression '{Cron}' for job '{Name}' ({Id})", cronExpression, job.Name, job.Id);
            return;
        }

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(cronExpression, x => x.InTimeZone(TimeZoneInfo.Local))
            .Build();

        if (await scheduler.CheckExists(jobKey))
        {
            // Job exists — reschedule the trigger
            await scheduler.RescheduleJob(triggerKey, trigger);
            _logger.LogDebug("Rescheduled job '{Name}' ({Id})", job.Name, job.Id);
        }
        else
        {
            // New job — create job detail and schedule
            var jobDetail = JobBuilder.Create<CronchQuartzJob>()
                .WithIdentity(jobKey)
                .UsingJobData(CronchQuartzJob.JobIdKey, job.Id.ToString())
                .Build();

            await scheduler.ScheduleJob(jobDetail, trigger);
            _logger.LogDebug("Scheduled job '{Name}' ({Id})", job.Name, job.Id);
        }
    }
}

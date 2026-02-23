using cronch.Models;
using Quartz;
using Quartz.Impl.Matchers;

namespace cronch.Services;

public class JobSchedulingService(ILogger<JobSchedulingService> _logger, ISchedulerFactory _schedulerFactory)
{
    private const string TriggerGroupName = "cronch";
    private const string JobGroupName = "cronch";

    public bool IsRunning
    {
        get
        {
            try
            {
                var scheduler = _schedulerFactory.GetScheduler().GetAwaiter().GetResult();
                return scheduler.IsStarted && !scheduler.IsShutdown;
            }
            catch
            {
                return false;
            }
        }
    }

    public virtual void RefreshSchedules(IEnumerable<JobModel> allJobs)
    {
        var scheduler = _schedulerFactory.GetScheduler().GetAwaiter().GetResult();
        var enabledJobs = allJobs.Where(j => j.Enabled && !string.IsNullOrWhiteSpace(j.CronSchedule)).ToList();

        // Get all currently scheduled trigger keys in our group
        var existingTriggerKeys = scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(TriggerGroupName))
            .GetAwaiter().GetResult();

        var desiredTriggerKeys = new HashSet<string>(enabledJobs.Select(j => j.Id.ToString()));

        // Remove triggers for jobs that are no longer enabled or have been deleted
        foreach (var triggerKey in existingTriggerKeys)
        {
            if (!desiredTriggerKeys.Contains(triggerKey.Name))
            {
                scheduler.UnscheduleJob(triggerKey).GetAwaiter().GetResult();
                var jobKey = new JobKey(triggerKey.Name, JobGroupName);
                scheduler.DeleteJob(jobKey).GetAwaiter().GetResult();
                _logger.LogDebug("Unscheduled job {Id}", triggerKey.Name);
            }
        }

        // Add or update triggers for enabled jobs
        foreach (var job in enabledJobs)
        {
            try
            {
                ScheduleOrUpdateJob(scheduler, job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to schedule job '{Name}' ({Id})", job.Name, job.Id);
            }
        }
    }

    public virtual DateTimeOffset? GetNextExecution(Guid jobId)
    {
        try
        {
            var scheduler = _schedulerFactory.GetScheduler().GetAwaiter().GetResult();
            var triggerKey = new TriggerKey(jobId.ToString(), TriggerGroupName);
            var trigger = scheduler.GetTrigger(triggerKey).GetAwaiter().GetResult();
            return trigger?.GetNextFireTimeUtc();
        }
        catch
        {
            return null;
        }
    }

    private void ScheduleOrUpdateJob(IScheduler scheduler, JobModel job)
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

        if (scheduler.CheckExists(jobKey).GetAwaiter().GetResult())
        {
            // Job exists — reschedule the trigger
            scheduler.RescheduleJob(triggerKey, trigger).GetAwaiter().GetResult();
            _logger.LogDebug("Rescheduled job '{Name}' ({Id})", job.Name, job.Id);
        }
        else
        {
            // New job — create job detail and schedule
            var jobDetail = JobBuilder.Create<CronchQuartzJob>()
                .WithIdentity(jobKey)
                .UsingJobData(CronchQuartzJob.JobIdKey, job.Id.ToString())
                .Build();

            scheduler.ScheduleJob(jobDetail, trigger).GetAwaiter().GetResult();
            _logger.LogDebug("Scheduled job '{Name}' ({Id})", job.Name, job.Id);
        }
    }
}

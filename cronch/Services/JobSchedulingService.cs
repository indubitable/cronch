using cronch.Models;
using Cronos;

namespace cronch.Services;

public class JobSchedulingService(ILogger<JobSchedulingService> _logger, JobConfigService _jobConfigService, JobExecutionService _jobExecutionService)
{
    private record struct ScheduledRun(DateTimeOffset When, Guid JobId);
    private class EarliestScheduledRun : IComparer<ScheduledRun>
    {
        public int Compare(ScheduledRun x, ScheduledRun y)
        {
            var v = x.When.CompareTo(y.When);
            return (v != 0 ? v : x.JobId.CompareTo(y.JobId));
        }
    }

    private Thread? _schedulerThread;
    private readonly object _syncLock = new();

    private bool _stopRequested;
    private bool _refreshRequested;

    public virtual void StartSchedulingRuns()
    {
        if (_schedulerThread != null)
        {
            throw new InvalidOperationException("Cannot start scheduling when scheduling is already running");
        }

        _stopRequested = false;
        _refreshRequested = true;
        _schedulerThread = new Thread(RunScheduling)
        {
            IsBackground = true
        };
        _schedulerThread.Start();
    }

    public virtual void StopSchedulingRuns(bool waitForStop)
    {
        var thread = _schedulerThread;
        if (thread == null || !thread.IsAlive) return;

        lock (_syncLock)
        {
            _stopRequested = true;
        }

        if (waitForStop)
        {
            thread.Join();
        }
    }

    public virtual void RefreshSchedules()
    {
        lock (_syncLock)
        {
            _refreshRequested = true;
        }
    }

    private void RunScheduling()
    {
        IEnumerable<JobModel> cachedEnabledJobs = [];
        var scheduleQueue = new SortedSet<ScheduledRun>(new EarliestScheduledRun());

        while (true)
        {
            // Check for cross-thread changes
            lock (_syncLock)
            {
                if (_stopRequested) break;

                if (_refreshRequested)
                {
                    _refreshRequested = false;
                    var enabledJobs = _jobConfigService.GetAllJobs().Where(j => j.Enabled).ToList();

                    // Delete any scheduled runs for jobs that have been disabled
                    var newlyDisabledJobs = cachedEnabledJobs.Where(oldJob => !enabledJobs.Any(newJob => newJob.Id == oldJob.Id));
                    newlyDisabledJobs.ToList().ForEach(disabledJob => scheduleQueue.RemoveWhere(q => q.JobId == disabledJob.Id));

                    // Delete any scheduled runs for jobs that have new schedules
                    var rescheduledJobs = cachedEnabledJobs.Where(oldJob => !enabledJobs.FirstOrDefault(newJob => newJob.Id == oldJob.Id)?.CronSchedule.Equals(oldJob.CronSchedule, StringComparison.InvariantCulture) ?? false);
                    rescheduledJobs.ToList().ForEach(rescheduledJob => scheduleQueue.RemoveWhere(q => q.JobId == rescheduledJob.Id));

                    // Add schedules for all enabled jobs... duplicates are ignored by the SortedSet<T>
                    foreach (var job in enabledJobs)
                    {
                        try
                        {
                            var cron = CronExpression.Parse(job.CronSchedule, CronFormat.IncludeSeconds);
                            cron.GetOccurrences(DateTimeOffset.Now, DateTimeOffset.Now.AddSeconds(10), TimeZoneInfo.Local)
                                .ToList()
                                .ForEach(occurrence => scheduleQueue.Add(new ScheduledRun(occurrence, job.Id)));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unable to schedule job '{Name}' ({Id})", job.Name, job.Id);
                        }
                    }

                    // Update the cache
                    cachedEnabledJobs = enabledJobs;
                }
            }

            while (scheduleQueue.Count > 0 && scheduleQueue.First().When <= DateTimeOffset.Now)
            {
                var exec = scheduleQueue.First();
                var job = cachedEnabledJobs.FirstOrDefault(j => j.Id == exec.JobId);
                if (job != null)
                {
                    _jobExecutionService.ExecuteJob(job, ExecutionModel.ExecutionReason.Scheduled);
                }
                else
                {
                    _logger.LogWarning("Cannot execute unknown job {Id}", exec.JobId);
                }
                scheduleQueue.Remove(exec);
            }

            // Wait a short while, and then continue on
            Thread.Sleep(300);
        }

        // Bye!
        _schedulerThread = null;
    }
}

using cronch.Models;
using Cronos;

namespace cronch.Services;

public class JobSchedulingService(ILogger<JobSchedulingService> _logger, JobExecutionService _jobExecutionService)
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

    public bool IsRunning => _schedulerThread?.IsAlive == true;

    private bool _stopRequested;
    private bool _refreshRequested;
    private List<JobModel> _rawEnabledJobs = [];
    private IEnumerable<JobModel> _cachedEnabledJobs = [];

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

    public virtual void RefreshSchedules(IEnumerable<JobModel> allJobs)
    {
        lock (_syncLock)
        {
            _refreshRequested = true;
            _rawEnabledJobs = new List<JobModel>(allJobs.Where(j => j.Enabled));
        }
    }

    public virtual DateTimeOffset? GetNextExecution(Guid jobId)
    {
        string? cron;
        lock (_syncLock)
        {
            // Use the raw enabled jobs because we don't need to wait for processing in this (informational) case
            cron = _rawEnabledJobs.FirstOrDefault(j => j.Id == jobId)?.CronSchedule;
        }
        if (!string.IsNullOrWhiteSpace(cron))
        {
            return CronExpression.Parse(cron, CronFormat.IncludeSeconds).GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        }
        return null;
    }

    private void RunScheduling()
    {
        const int scheduleFutureSeconds = 10;
        const int addToScheduleAfterSeconds = 5;

        var scheduleQueue = new SortedSet<ScheduledRun>(new EarliestScheduledRun());
        var lastScheduled = DateTimeOffset.MinValue;

        while (true)
        {
            // Check for cross-thread changes
            lock (_syncLock)
            {
                if (_stopRequested) break;

                if (_refreshRequested)
                {
                    _refreshRequested = false;
                    lastScheduled = DateTimeOffset.MinValue;

                    // Delete any scheduled runs for jobs that have been disabled
                    var newlyDisabledJobs = _cachedEnabledJobs.Where(oldJob => !_rawEnabledJobs.Any(newJob => newJob.Id == oldJob.Id));
                    newlyDisabledJobs.ToList().ForEach(disabledJob => scheduleQueue.RemoveWhere(q => q.JobId == disabledJob.Id));

                    // Delete any scheduled runs for jobs that have new schedules
                    var rescheduledJobs = _cachedEnabledJobs.Where(oldJob => !string.Equals(_rawEnabledJobs.FirstOrDefault(newJob => newJob.Id == oldJob.Id)?.CronSchedule, oldJob.CronSchedule, StringComparison.InvariantCulture));
                    rescheduledJobs.ToList().ForEach(rescheduledJob => scheduleQueue.RemoveWhere(q => q.JobId == rescheduledJob.Id));

                    // Update the cache
                    _cachedEnabledJobs = new List<JobModel>(_rawEnabledJobs);
                }
            }

            if (DateTimeOffset.UtcNow > lastScheduled.AddSeconds(addToScheduleAfterSeconds))
            {
                // Add schedules for all enabled jobs... duplicates are ignored by the SortedSet<T>
                foreach (var job in _cachedEnabledJobs)
                {
                    if (string.IsNullOrWhiteSpace(job.CronSchedule)) continue;
                    try
                    {
                        var cron = CronExpression.Parse(job.CronSchedule, CronFormat.IncludeSeconds);
                        cron.GetOccurrences(DateTimeOffset.Now, DateTimeOffset.Now.AddSeconds(scheduleFutureSeconds), TimeZoneInfo.Local)
                            .ToList()
                            .ForEach(occurrence => scheduleQueue.Add(new ScheduledRun(occurrence, job.Id)));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to schedule job '{Name}' ({Id})", job.Name, job.Id);
                    }
                }

                lastScheduled = DateTimeOffset.UtcNow;
            }

            List<Guid> executedJobs = [];
            while (scheduleQueue.Count > 0 && scheduleQueue.First().When <= DateTimeOffset.Now)
            {
                var exec = scheduleQueue.First();
                if (executedJobs.Contains(exec.JobId))
                {
                    // This really shouldn't happen except when debugging or possibly under unreasonable load
                    _logger.LogWarning("Skipping apparent duplicate scheduled execution for job {Id}", exec.JobId);
                    continue;
                }
                var job = _cachedEnabledJobs.FirstOrDefault(j => j.Id == exec.JobId);
                if (job != null)
                {
                    _jobExecutionService.ExecuteJob(job, ExecutionReason.Scheduled);
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

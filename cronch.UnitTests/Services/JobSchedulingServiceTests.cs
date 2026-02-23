using cronch.Models;
using cronch.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Quartz.Impl;
using System.Collections.Specialized;

namespace cronch.UnitTests.Services;

[TestClass]
public class JobSchedulingServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private JobSchedulingService _jobSchedulingService = null!;
    private ISchedulerFactory _schedulerFactory = null!;
    private IScheduler _scheduler = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Each test gets a uniquely named scheduler to avoid Quartz's global registry collision
        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"TestScheduler_{Guid.NewGuid():N}"
        };
        _schedulerFactory = new StdSchedulerFactory(properties);
        _scheduler = await _schedulerFactory.GetScheduler();
        await _scheduler.Start();

        _jobSchedulingService = new JobSchedulingService(
            Substitute.For<ILogger<JobSchedulingService>>(),
            _schedulerFactory);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_scheduler != null && _scheduler.IsStarted)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: false);
        }
    }

    // --- IsRunning ---

    [TestMethod]
    public async Task IsRunningShouldReturnTrueWhenSchedulerIsStarted()
    {
        Assert.IsTrue(await _jobSchedulingService.IsRunningAsync());
    }

    [TestMethod]
    public async Task IsRunningShouldReturnFalseAfterShutdown()
    {
        await _scheduler.Shutdown(waitForJobsToComplete: false);

        Assert.IsFalse(await _jobSchedulingService.IsRunningAsync());
    }

    // --- GetNextExecution ---

    [TestMethod]
    public async Task GetNextExecutionShouldReturnNullWhenJobIdIsNotScheduled()
    {
        var result = await _jobSchedulingService.GetNextExecutionAsync(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetNextExecutionShouldReturnFutureTimeForScheduledJob()
    {
        var jobId = Guid.NewGuid();
        // "* * * * * ?" fires every second in Quartz format
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * ?" };
        var before = DateTimeOffset.Now;

        await _jobSchedulingService.RefreshSchedulesAsync([job]);
        var result = await _jobSchedulingService.GetNextExecutionAsync(jobId);

        Assert.IsNotNull(result);
        // Quartz may return the current second boundary (inclusive), so allow >= before minus 1 second
        Assert.IsTrue(result >= before.AddSeconds(-1), "Next execution should be around or after now");
        Assert.IsTrue(result <= before.AddSeconds(2), "Next execution for an every-second cron should be within 2 seconds");
    }

    [TestMethod]
    public async Task GetNextExecutionShouldReturnNullAfterJobIsRemovedViaRefreshSchedules()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);
        Assert.IsNotNull(await _jobSchedulingService.GetNextExecutionAsync(jobId), "Precondition: job should have a next execution before removal");

        await _jobSchedulingService.RefreshSchedulesAsync([]);
        var result = await _jobSchedulingService.GetNextExecutionAsync(jobId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetNextExecutionShouldReturnNullAfterJobIsDisabledViaRefreshSchedules()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);
        Assert.IsNotNull(await _jobSchedulingService.GetNextExecutionAsync(jobId), "Precondition: job should have a next execution while enabled");

        await _jobSchedulingService.RefreshSchedulesAsync([new JobModel { Id = jobId, Enabled = false, CronSchedule = "* * * * * ?" }]);
        var result = await _jobSchedulingService.GetNextExecutionAsync(jobId);

        Assert.IsNull(result);
    }

    // --- RefreshSchedules ---

    [TestMethod]
    public async Task RefreshSchedulesShouldCreateTriggerForEnabledJob()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "0 0 12 * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);

        var triggerKey = new TriggerKey(jobId.ToString(), "cronch");
        var trigger = await _scheduler.GetTrigger(triggerKey);
        Assert.IsNotNull(trigger);
    }

    [TestMethod]
    public async Task RefreshSchedulesShouldNotCreateTriggerForDisabledJob()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = false, CronSchedule = "0 0 12 * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);

        var triggerKey = new TriggerKey(jobId.ToString(), "cronch");
        var trigger = await _scheduler.GetTrigger(triggerKey);
        Assert.IsNull(trigger);
    }

    [TestMethod]
    public async Task RefreshSchedulesShouldNotCreateTriggerForJobWithNoCronSchedule()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = null };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);

        var triggerKey = new TriggerKey(jobId.ToString(), "cronch");
        var trigger = await _scheduler.GetTrigger(triggerKey);
        Assert.IsNull(trigger);
    }

    [TestMethod]
    public async Task RefreshSchedulesShouldRemoveTriggerWhenJobIsRemoved()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "0 0 12 * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);

        var triggerKey = new TriggerKey(jobId.ToString(), "cronch");
        Assert.IsNotNull(await _scheduler.GetTrigger(triggerKey), "Precondition");

        await _jobSchedulingService.RefreshSchedulesAsync([]);

        Assert.IsNull(await _scheduler.GetTrigger(triggerKey));
    }

    [TestMethod]
    public async Task RefreshSchedulesShouldUpdateTriggerWhenCronChanges()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "0 0 12 * * ?" };

        await _jobSchedulingService.RefreshSchedulesAsync([job]);

        var triggerKey = new TriggerKey(jobId.ToString(), "cronch");
        var trigger1 = await _scheduler.GetTrigger(triggerKey) as ICronTrigger;
        Assert.IsNotNull(trigger1);
        Assert.AreEqual("0 0 12 * * ?", trigger1.CronExpressionString);

        // Change the cron schedule
        var updatedJob = new JobModel { Id = jobId, Enabled = true, CronSchedule = "0 30 6 * * ?" };
        await _jobSchedulingService.RefreshSchedulesAsync([updatedJob]);

        var trigger2 = await _scheduler.GetTrigger(triggerKey) as ICronTrigger;
        Assert.IsNotNull(trigger2);
        Assert.AreEqual("0 30 6 * * ?", trigger2.CronExpressionString);
    }
}

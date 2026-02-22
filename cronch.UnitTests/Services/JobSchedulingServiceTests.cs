using cronch.Models;
using cronch.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace cronch.UnitTests.Services;

[TestClass]
public class JobSchedulingServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    private JobSchedulingService _jobSchedulingService = null!;
    private JobExecutionService _jobExecutionService = null!;

    [TestInitialize]
    public void Setup()
    {
        _jobExecutionService = Substitute.For<JobExecutionService>(
            Substitute.For<ILogger<JobExecutionService>>(),
            null!,
            null!);
        _jobSchedulingService = new JobSchedulingService(
            Substitute.For<ILogger<JobSchedulingService>>(),
            _jobExecutionService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _jobSchedulingService.StopSchedulingRuns(waitForStop: true);
    }

    // --- GetNextExecution ---

    [TestMethod]
    public void GetNextExecutionShouldReturnNullWhenJobIdIsNotInEnabledJobs()
    {
        _jobSchedulingService.RefreshSchedules([]);

        var result = _jobSchedulingService.GetNextExecution(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnNullWhenJobIsDisabled()
    {
        var jobId = Guid.NewGuid();
        var disabledJob = new JobModel { Id = jobId, Enabled = false, CronSchedule = "* * * * * *" };

        _jobSchedulingService.RefreshSchedules([disabledJob]);

        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnNullWhenCronScheduleIsEmpty()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "" };

        _jobSchedulingService.RefreshSchedules([job]);

        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnNullWhenCronScheduleIsWhitespace()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "   " };

        _jobSchedulingService.RefreshSchedules([job]);

        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnFutureTimeForValidCronSchedule()
    {
        var jobId = Guid.NewGuid();
        // "* * * * * *" fires every second, so there is always a next occurrence within 1 second
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * *" };
        var before = DateTimeOffset.Now;

        _jobSchedulingService.RefreshSchedules([job]);
        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNotNull(result);
        Assert.IsTrue(result > before, "Next execution should be in the future");
        Assert.IsTrue(result <= before.AddSeconds(2), "Next execution for an every-second cron should be within 2 seconds");
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnNullAfterJobIsRemovedViaRefreshSchedules()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * *" };

        _jobSchedulingService.RefreshSchedules([job]);
        Assert.IsNotNull(_jobSchedulingService.GetNextExecution(jobId), "Precondition: job should have a next execution before removal");

        _jobSchedulingService.RefreshSchedules([]);
        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetNextExecutionShouldReturnNullAfterJobIsDisabledViaRefreshSchedules()
    {
        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, CronSchedule = "* * * * * *" };

        _jobSchedulingService.RefreshSchedules([job]);
        Assert.IsNotNull(_jobSchedulingService.GetNextExecution(jobId), "Precondition: job should have a next execution while enabled");

        _jobSchedulingService.RefreshSchedules([new JobModel { Id = jobId, Enabled = false, CronSchedule = "* * * * * *" }]);
        var result = _jobSchedulingService.GetNextExecution(jobId);

        Assert.IsNull(result);
    }

    // --- RunScheduling ---

    [TestMethod]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunSchedulingShouldExecuteScheduledJob()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        var executed = new ManualResetEventSlim(false);
        _jobExecutionService
            .When(s => s.ExecuteJob(Arg.Any<JobModel>(), Arg.Any<ExecutionReason>(), Arg.Any<int>()))
            .Do(_ => executed.Set());

        var job = new JobModel { Id = Guid.NewGuid(), Enabled = true, Name = "Test", CronSchedule = "* * * * * *" };
        _jobSchedulingService.RefreshSchedules([job]);
        _jobSchedulingService.StartSchedulingRuns();

        Assert.IsTrue(executed.Wait(8000, ct), "Job should have been executed within the timeout");
    }

    [TestMethod]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunSchedulingShouldNotExecuteJobAfterDisabledViaRefreshSchedules()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        var firstExecution = new ManualResetEventSlim(false);
        _jobExecutionService
            .When(s => s.ExecuteJob(Arg.Any<JobModel>(), Arg.Any<ExecutionReason>(), Arg.Any<int>()))
            .Do(_ => firstExecution.Set());

        var job = new JobModel { Id = Guid.NewGuid(), Enabled = true, Name = "Test", CronSchedule = "* * * * * *" };
        _jobSchedulingService.RefreshSchedules([job]);
        _jobSchedulingService.StartSchedulingRuns();

        Assert.IsTrue(firstExecution.Wait(8000, ct), "Precondition: job should have executed at least once");

        _jobSchedulingService.RefreshSchedules([]);

        // Wait for the scheduler to process the refresh (multiple cycles at 300ms each)
        await Task.Delay(1000, ct);
        _jobExecutionService.ClearReceivedCalls();

        await Task.Delay(2000, ct);

        _jobExecutionService.DidNotReceive().ExecuteJob(Arg.Any<JobModel>(), Arg.Any<ExecutionReason>(), Arg.Any<int>());
    }

    [TestMethod]
    [Timeout(10000, CooperativeCancellation = true)]
    public async Task RunSchedulingShouldNotExecuteJobOnOldScheduleAfterCronChangedViaRefreshSchedules()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        var firstExecution = new ManualResetEventSlim(false);
        _jobExecutionService
            .When(s => s.ExecuteJob(Arg.Any<JobModel>(), Arg.Any<ExecutionReason>(), Arg.Any<int>()))
            .Do(_ => firstExecution.Set());

        var jobId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Enabled = true, Name = "Test", CronSchedule = "* * * * * *" };
        _jobSchedulingService.RefreshSchedules([job]);
        _jobSchedulingService.StartSchedulingRuns();

        Assert.IsTrue(firstExecution.Wait(8000, ct), "Precondition: job should have executed at least once");

        // Change cron to one that won't fire during this test (midnight on January 1st)
        var rescheduledJob = new JobModel { Id = jobId, Enabled = true, Name = "Test", CronSchedule = "0 0 0 1 1 *" };
        _jobSchedulingService.RefreshSchedules([rescheduledJob]);

        // Wait for the scheduler to process the refresh (multiple cycles at 300ms each)
        await Task.Delay(1000, ct);
        _jobExecutionService.ClearReceivedCalls();

        await Task.Delay(2000, ct);

        _jobExecutionService.DidNotReceive().ExecuteJob(Arg.Any<JobModel>(), Arg.Any<ExecutionReason>(), Arg.Any<int>());
    }
}

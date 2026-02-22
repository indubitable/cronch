using cronch.Models;
using cronch.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace cronch.UnitTests.Services;

[TestClass]
public class JobSchedulingServiceTests
{
    private JobSchedulingService _jobSchedulingService = null!;

    [TestInitialize]
    public void Setup()
    {
        _jobSchedulingService = new JobSchedulingService(
            Substitute.For<ILogger<JobSchedulingService>>(),
            null!);
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
}

using cronch.Models;
using cronch.Models.Persistence;
using cronch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;

namespace cronch.UnitTests.Services;

[TestClass]
public class JobConfigServiceTests
{
    private ConfigPersistenceService _configPersistence = null!;
    private JobSchedulingService _jobScheduling = null!;
    private JobConfigService _jobConfigService = null!;

    [TestInitialize]
    public void Setup()
    {
        _configPersistence = Substitute.For<ConfigPersistenceService>(
            Substitute.For<ILogger<ConfigPersistenceService>>(),
            Substitute.For<IConfiguration>());

        // JobSchedulingService takes ISchedulerFactory; null! is safe here
        // because all JobSchedulingService methods are virtual and will be intercepted by NSubstitute.
        _jobScheduling = Substitute.For<JobSchedulingService>(
            Substitute.For<ILogger<JobSchedulingService>>(),
            Substitute.For<ISchedulerFactory>());

        _jobConfigService = new JobConfigService(_configPersistence, _jobScheduling);
    }

    // --- GetAllJobs ---

    [TestMethod]
    public void GetAllJobsShouldReturnEmptyListWhenNoConfigExists()
    {
        _configPersistence.Load().Returns((ConfigPersistenceModel?)null);

        var result = _jobConfigService.GetAllJobs();

        Assert.IsEmpty(result);
    }

    // --- CreateJob ---

    [TestMethod]
    public async Task CreateJobShouldAssignNewGuidWhenAssignNewIdIsTrue()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        var job = new JobModel { Id = Guid.Empty, Name = "Test", Executor = "bash", Script = "" };

        await _jobConfigService.CreateJobAsync(job, assignNewId: true);

        Assert.AreNotEqual(Guid.Empty, job.Id);
    }

    [TestMethod]
    public async Task CreateJobShouldThrowWhenIdAlreadyExistsAndAssignNewIdIsFalse()
    {
        var existingId = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = existingId }]
        });
        var job = new JobModel { Id = existingId, Name = "Duplicate" };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobConfigService.CreateJobAsync(job, assignNewId: false));
    }

    [TestMethod]
    public async Task CreateJobShouldRefreshSchedulesAfterSaving()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        var job = new JobModel { Id = Guid.NewGuid(), Name = "Test", Executor = "bash", Script = "" };

        await _jobConfigService.CreateJobAsync(job, assignNewId: false);

        await _jobScheduling.Received(1).RefreshSchedulesAsync(Arg.Any<IEnumerable<JobModel>>());
    }

    // --- UpdateJob ---

    [TestMethod]
    public async Task UpdateJobShouldThrowWhenJobDoesNotExist()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobConfigService.UpdateJobAsync(new JobModel { Id = Guid.NewGuid() }));

    }

    [TestMethod]
    public async Task UpdateJobShouldRefreshSchedulesAfterSaving()
    {
        var id = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = id }]
        });

        await _jobConfigService.UpdateJobAsync(new JobModel { Id = id, Name = "Updated" });

        await _jobScheduling.Received(1).RefreshSchedulesAsync(Arg.Any<IEnumerable<JobModel>>());
    }

    // --- DeleteJob ---

    [TestMethod]
    public async Task DeleteJobShouldThrowWhenJobDoesNotExist()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _jobConfigService.DeleteJobAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task DeleteJobShouldRefreshSchedulesAfterSaving()
    {
        var id = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = id }]
        });

        await _jobConfigService.DeleteJobAsync(id);

        await _jobScheduling.Received(1).RefreshSchedulesAsync(Arg.Any<IEnumerable<JobModel>>());
    }
}

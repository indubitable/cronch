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
    public void CreateJobShouldAssignNewGuidWhenAssignNewIdIsTrue()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        var job = new JobModel { Id = Guid.Empty, Name = "Test", Executor = "bash", Script = "" };

        _jobConfigService.CreateJob(job, assignNewId: true);

        Assert.AreNotEqual(Guid.Empty, job.Id);
    }

    [TestMethod]
    public void CreateJobShouldThrowWhenIdAlreadyExistsAndAssignNewIdIsFalse()
    {
        var existingId = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = existingId }]
        });
        var job = new JobModel { Id = existingId, Name = "Duplicate" };

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _jobConfigService.CreateJob(job, assignNewId: false));
    }

    [TestMethod]
    public void CreateJobShouldRefreshSchedulesAfterSaving()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());
        var job = new JobModel { Id = Guid.NewGuid(), Name = "Test", Executor = "bash", Script = "" };

        _jobConfigService.CreateJob(job, assignNewId: false);

        _jobScheduling.Received(1).RefreshSchedules(Arg.Any<IEnumerable<JobModel>>());
    }

    // --- UpdateJob ---

    [TestMethod]
    public void UpdateJobShouldThrowWhenJobDoesNotExist()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _jobConfigService.UpdateJob(new JobModel { Id = Guid.NewGuid() }));
    }

    [TestMethod]
    public void UpdateJobShouldRefreshSchedulesAfterSaving()
    {
        var id = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = id }]
        });

        _jobConfigService.UpdateJob(new JobModel { Id = id, Name = "Updated" });

        _jobScheduling.Received(1).RefreshSchedules(Arg.Any<IEnumerable<JobModel>>());
    }

    // --- DeleteJob ---

    [TestMethod]
    public void DeleteJobShouldThrowWhenJobDoesNotExist()
    {
        _configPersistence.Load().Returns(new ConfigPersistenceModel());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _jobConfigService.DeleteJob(Guid.NewGuid()));
    }

    [TestMethod]
    public void DeleteJobShouldRefreshSchedulesAfterSaving()
    {
        var id = Guid.NewGuid();
        _configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = [new JobPersistenceModel { Id = id }]
        });

        _jobConfigService.DeleteJob(id);

        _jobScheduling.Received(1).RefreshSchedules(Arg.Any<IEnumerable<JobModel>>());
    }
}

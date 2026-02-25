using cronch.Models;
using cronch.Models.Persistence;
using cronch.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace cronch.IntegrationTests;

/// <summary>
/// In-memory SQLite persistence service that skips filesystem operations in
/// DeleteExecution, so cleanup tests stay focused on database-level behavior.
/// </summary>
internal sealed class InMemoryDbOnlyExecutionPersistenceService : ExecutionPersistenceService, IDisposable
{
    private readonly SqliteConnection _anchorConnection;

    public InMemoryDbOnlyExecutionPersistenceService()
        : base(NullLogger<ExecutionPersistenceService>.Instance, null!)
    {
        SQLitePCL.Batteries_V2.Init();
        var dbId = Guid.NewGuid().ToString("N");
        _connectionString = $"Data Source=file:testdb_{dbId}?mode=memory&cache=shared";

        _anchorConnection = new SqliteConnection(_connectionString);
        _anchorConnection.Open();

        InitializeTables();
    }

    public override void DeleteExecution(ExecutionModel execution)
    {
        using var db = new SqliteConnection(_connectionString);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM Execution WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", execution.Id.ToString("D").ToUpperInvariant());
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _anchorConnection.Dispose();
}

[TestClass]
public class CleanupServiceTests
{
    private InMemoryDbOnlyExecutionPersistenceService _db = null!;

    [TestInitialize]
    public void Setup() => _db = new InMemoryDbOnlyExecutionPersistenceService();

    [TestCleanup]
    public void Cleanup() => _db.Dispose();

    // --- CleanUpExecutionsByAge ---

    [TestMethod]
    public void CleanUpExecutionsByAgeShouldDoNothingWhenSettingIsNull()
    {
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-30));
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-60));
        var sut = CreateCleanupService(settings: new SettingsModel { DeleteHistoricalRunsAfterDays = null });

        sut.CleanUpExecutionsByAge(_db);

        Assert.HasCount(2, _db.GetRecentExecutions(100, null, null).ToList());
    }

    [TestMethod]
    public void CleanUpExecutionsByAgeShouldDeleteExecutionsOlderThanCutoff()
    {
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-15)); // older than 7-day cutoff → should be deleted
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-1));  // recent → should stay
        var sut = CreateCleanupService(settings: new SettingsModel { DeleteHistoricalRunsAfterDays = 7 });

        sut.CleanUpExecutionsByAge(_db);

        var remaining = _db.GetRecentExecutions(100, null, null).ToList();
        Assert.HasCount(1, remaining);
        Assert.IsTrue(remaining[0].StartedOn > DateTimeOffset.UtcNow.AddDays(-2));
    }

    [TestMethod]
    public void CleanUpExecutionsByAgeShouldNotDeleteRecentExecutions()
    {
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-1));
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-3));
        AddExecution(startedOn: DateTimeOffset.UtcNow.AddDays(-6));
        var sut = CreateCleanupService(settings: new SettingsModel { DeleteHistoricalRunsAfterDays = 7 });

        sut.CleanUpExecutionsByAge(_db);

        Assert.HasCount(3, _db.GetRecentExecutions(100, null, null).ToList());
    }

    // --- CleanUpExecutionsByCount ---

    [TestMethod]
    public void CleanUpExecutionsByCountShouldDoNothingWhenSettingIsNull()
    {
        var jobId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            AddExecution(jobId: jobId);
        }

        var sut = CreateCleanupService(
            settings: new SettingsModel { DeleteHistoricalRunsAfterCount = null },
            jobIds: [jobId]);

        sut.CleanUpExecutionsByCount(_db);

        Assert.HasCount(5, _db.GetRecentExecutions(100, jobId, null).ToList());
    }

    [TestMethod]
    public void CleanUpExecutionsByCountShouldDeleteOldestExecutionsBeyondLimit()
    {
        var jobId = Guid.NewGuid();
        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        // Add 5 executions with known order: index 0 is oldest, index 4 is newest
        var ids = Enumerable.Range(0, 5)
            .Select(i => AddExecution(jobId: jobId, startedOn: baseTime.AddMinutes(i)).Id)
            .ToList();
        var sut = CreateCleanupService(
            settings: new SettingsModel { DeleteHistoricalRunsAfterCount = 3 },
            jobIds: [jobId]);

        sut.CleanUpExecutionsByCount(_db);

        var remaining = _db.GetRecentExecutions(100, jobId, null).Select(e => e.Id).ToHashSet();
        Assert.HasCount(3, remaining);
        // The 2 oldest should be gone
        Assert.DoesNotContain(ids[0], remaining, "Oldest execution should have been deleted");
        Assert.DoesNotContain(ids[1], remaining, "Second-oldest execution should have been deleted");
        // The 3 newest should remain
        Assert.Contains(ids[2], remaining);
        Assert.Contains(ids[3], remaining);
        Assert.Contains(ids[4], remaining);
    }

    [TestMethod]
    public void CleanUpExecutionsByCountShouldNotDeleteWhenCountIsWithinLimit()
    {
        var jobId = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
        {
            AddExecution(jobId: jobId);
        }

        var sut = CreateCleanupService(
            settings: new SettingsModel { DeleteHistoricalRunsAfterCount = 5 },
            jobIds: [jobId]);

        sut.CleanUpExecutionsByCount(_db);

        Assert.HasCount(3, _db.GetRecentExecutions(100, jobId, null).ToList());
    }

    [TestMethod]
    public void CleanUpExecutionsByCountShouldNotTouchExecutionsForUnconfiguredJobs()
    {
        var configuredJobId = Guid.NewGuid();
        var unconfiguredJobId = Guid.NewGuid();

        var baseTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        for (var i = 0; i < 4; i++)
        {
            AddExecution(jobId: configuredJobId, startedOn: baseTime.AddMinutes(i));
        }
        for (var i = 0; i < 4; i++)
        {
            AddExecution(jobId: unconfiguredJobId, startedOn: baseTime.AddMinutes(i));
        }

        // Only configuredJobId is known to JobConfigService
        var sut = CreateCleanupService(
            settings: new SettingsModel { DeleteHistoricalRunsAfterCount = 2 },
            jobIds: [configuredJobId]);

        sut.CleanUpExecutionsByCount(_db);

        Assert.HasCount(2, _db.GetRecentExecutions(100, configuredJobId, null).ToList(), "Configured job should be trimmed to limit");
        Assert.HasCount(4, _db.GetRecentExecutions(100, unconfiguredJobId, null).ToList(), "Unconfigured job should be untouched");
    }

    // --- Helpers ---

    private ExecutionModel AddExecution(Guid? jobId = null, DateTimeOffset? startedOn = null)
    {
        var execution = new ExecutionModel
        {
            Id = Guid.NewGuid(),
            JobId = jobId ?? Guid.NewGuid(),
            JobName = "Test Job",
            StartedOn = startedOn ?? DateTimeOffset.UtcNow,
            StartReason = ExecutionReason.Scheduled,
            Status = ExecutionStatus.CompletedAsSuccess,
            CompletedOn = DateTimeOffset.UtcNow,
            ExitCode = 0,
            StopReason = TerminationReason.Exited,
        };
        _db.AddExecution(execution);
        return execution;
    }

    private CleanupService CreateCleanupService(SettingsModel settings, params Guid[] jobIds)
    {
        var settingsService = Substitute.For<SettingsService>(
            Substitute.For<ConfigPersistenceService>(
                Substitute.For<ILogger<ConfigPersistenceService>>(),
                Substitute.For<IConfiguration>()));
        settingsService.LoadSettings().Returns(settings);

        var configPersistence = Substitute.For<ConfigPersistenceService>(
            Substitute.For<ILogger<ConfigPersistenceService>>(),
            Substitute.For<IConfiguration>());
        configPersistence.Load().Returns(new ConfigPersistenceModel
        {
            Jobs = jobIds.Select(id => new JobPersistenceModel { Id = id }).ToList()
        });
        // JobSchedulingService is not invoked by GetAllJobs; null! is safe here
        var jobConfigService = new JobConfigService(configPersistence, null!);

        // IServiceProvider is not used by CleanUpExecutionsByAge or CleanUpExecutionsByCount
        return new CleanupService(
            Substitute.For<ILogger<CleanupService>>(),
            jobConfigService,
            settingsService,
            null!);
    }
}

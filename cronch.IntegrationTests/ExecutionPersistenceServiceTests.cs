using cronch.Models;
using cronch.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace cronch.IntegrationTests;

/// <summary>
/// Subclass that wires up an isolated named in-memory SQLite database.
/// A single anchor connection is held open for the lifetime of the instance
/// so the in-memory database is not destroyed between operations.
/// </summary>
internal sealed class InMemoryExecutionPersistenceService : ExecutionPersistenceService, IDisposable
{
    private readonly SqliteConnection _anchorConnection;

    public InMemoryExecutionPersistenceService()
        : base(NullLogger<ExecutionPersistenceService>.Instance, null!)
    {
        SQLitePCL.Batteries_V2.Init();
        var dbId = Guid.NewGuid().ToString("N");
        _connectionString = $"Data Source=file:testdb_{dbId}?mode=memory&cache=shared";

        _anchorConnection = new SqliteConnection(_connectionString);
        _anchorConnection.Open();

        InitializeTables();
    }

    public void Dispose() => _anchorConnection.Dispose();
}

[TestClass]
public class ExecutionPersistenceServiceTests
{
    private InMemoryExecutionPersistenceService _service = null!;

    [TestInitialize]
    public void Setup() => _service = new InMemoryExecutionPersistenceService();

    [TestCleanup]
    public void Cleanup() => _service.Dispose();

    // --- Add / Get round-trips ---

    [TestMethod]
    public void AddAndGetExecutionShouldRoundTripAllFields()
    {
        var startedOn = DateTimeOffset.UtcNow;
        var original = new ExecutionModel
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            JobName = "My Job",
            StartedOn = startedOn,
            StartReason = ExecutionReason.Manual,
            Status = ExecutionStatus.CompletedAsSuccess,
            CompletedOn = startedOn.AddSeconds(5),
            ExitCode = 0,
            StopReason = TerminationReason.Exited,
        };

        _service.AddExecution(original);
        var retrieved = _service.GetExecution(original.Id);

        Assert.IsNotNull(retrieved);
        Assert.AreEqual(original.Id, retrieved.Id);
        Assert.AreEqual(original.JobId, retrieved.JobId);
        Assert.AreEqual(original.JobName, retrieved.JobName);
        Assert.AreEqual(original.StartedOn.ToUniversalTime(), retrieved.StartedOn);
        Assert.AreEqual(original.StartReason, retrieved.StartReason);
        Assert.AreEqual(original.Status, retrieved.Status);
        Assert.AreEqual(original.CompletedOn?.ToUniversalTime(), retrieved.CompletedOn);
        Assert.AreEqual(original.ExitCode, retrieved.ExitCode);
        Assert.AreEqual(original.StopReason, retrieved.StopReason);
    }

    [TestMethod]
    public void AddAndGetExecutionShouldPreserveNullOptionalFields()
    {
        var original = new ExecutionModel
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            JobName = "Running Job",
            StartedOn = DateTimeOffset.UtcNow,
            StartReason = ExecutionReason.Scheduled,
            Status = ExecutionStatus.Running,
            CompletedOn = null,
            ExitCode = null,
            StopReason = null,
        };

        _service.AddExecution(original);
        var retrieved = _service.GetExecution(original.Id);

        Assert.IsNotNull(retrieved);
        Assert.IsNull(retrieved.CompletedOn);
        Assert.IsNull(retrieved.ExitCode);
        Assert.IsNull(retrieved.StopReason);
    }

    [TestMethod]
    public void GetExecutionShouldReturnNullForUnknownId()
    {
        var result = _service.GetExecution(Guid.NewGuid());

        Assert.IsNull(result);
    }

    // --- GetRecentExecutions ---

    [TestMethod]
    public void GetRecentExecutionsShouldFilterByJobId()
    {
        var targetJobId = Guid.NewGuid();
        AddExecution(jobId: targetJobId);
        AddExecution(jobId: targetJobId);
        AddExecution(jobId: Guid.NewGuid());

        var results = _service.GetRecentExecutions(10, targetJobId, null).ToList();

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(e => e.JobId == targetJobId));
    }

    [TestMethod]
    public void GetRecentExecutionsShouldFilterByStatus()
    {
        AddExecution(status: ExecutionStatus.CompletedAsSuccess);
        AddExecution(status: ExecutionStatus.CompletedAsSuccess);
        AddExecution(status: ExecutionStatus.CompletedAsError);

        var results = _service.GetRecentExecutions(10, null, ExecutionStatus.CompletedAsSuccess).ToList();

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(e => e.Status == ExecutionStatus.CompletedAsSuccess));
    }

    [TestMethod]
    public void GetRecentExecutionsShouldRespectMaxCount()
    {
        for (var i = 0; i < 5; i++)
        {
            AddExecution();
        }

        var results = _service.GetRecentExecutions(3, null, null).ToList();

        Assert.HasCount(3, results);
    }

    // --- GetExecutionStatistics ---

    [TestMethod]
    public void GetExecutionStatisticsShouldCountByStatus()
    {
        var from = DateTimeOffset.UtcNow.AddSeconds(-1);
        AddExecution(status: ExecutionStatus.CompletedAsSuccess);
        AddExecution(status: ExecutionStatus.CompletedAsSuccess);
        AddExecution(status: ExecutionStatus.CompletedAsError);
        AddExecution(status: ExecutionStatus.CompletedAsWarning);
        var to = DateTimeOffset.UtcNow.AddSeconds(1);

        var stats = _service.GetExecutionStatistics(from, to);

        Assert.AreEqual(2, stats.Successes);
        Assert.AreEqual(1, stats.Errors);
        Assert.AreEqual(1, stats.Warnings);
    }

    // --- GetLatestExecutionsPerJob ---

    [TestMethod]
    public void GetLatestExecutionsPerJobShouldReturnMostRecentStartTime()
    {
        var jobId = Guid.NewGuid();
        var earlier = DateTimeOffset.UtcNow.AddMinutes(-5);
        var later = DateTimeOffset.UtcNow;
        AddExecution(jobId: jobId, startedOn: earlier);
        AddExecution(jobId: jobId, startedOn: later);

        var result = _service.GetLatestExecutionsPerJob();

        Assert.IsTrue(result.ContainsKey(jobId));
        Assert.AreEqual(later.ToUniversalTime(), result[jobId]);
    }

    // --- UpdateExecution ---

    [TestMethod]
    public void UpdateExecutionShouldPersistChanges()
    {
        var execution = MakeExecution(status: ExecutionStatus.Running);
        execution.CompletedOn = null;
        execution.StopReason = null;
        _service.AddExecution(execution);

        execution.Status = ExecutionStatus.CompletedAsSuccess;
        execution.CompletedOn = DateTimeOffset.UtcNow;
        execution.ExitCode = 0;
        execution.StopReason = TerminationReason.Exited;
        _service.UpdateExecution(execution);

        var retrieved = _service.GetExecution(execution.Id);
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, retrieved.Status);
        Assert.AreEqual(0, retrieved.ExitCode);
        Assert.AreEqual(TerminationReason.Exited, retrieved.StopReason);
        Assert.IsNotNull(retrieved.CompletedOn);
    }

    // --- RemoveAllDatabaseRecordsForJobId ---

    [TestMethod]
    public void RemoveAllDatabaseRecordsForJobIdShouldDeleteOnlyTargetJobRecords()
    {
        var targetJobId = Guid.NewGuid();
        var otherJobId = Guid.NewGuid();
        AddExecution(jobId: targetJobId);
        AddExecution(jobId: targetJobId);
        AddExecution(jobId: otherJobId);

        _service.RemoveAllDatabaseRecordsForJobId(targetJobId);

        Assert.IsEmpty(_service.GetRecentExecutions(10, targetJobId, null));
        Assert.HasCount(1, _service.GetRecentExecutions(10, otherJobId, null).ToList());
    }

    // --- GetOldestExecutionsAfterCount ---

    [TestMethod]
    public void GetOldestExecutionsAfterCountShouldSkipTheMostRecentOnes()
    {
        var jobId = Guid.NewGuid();
        var base_ = DateTimeOffset.UtcNow.AddMinutes(-10);
        // Add 5 executions with distinct, known timestamps (oldest first)
        var ids = Enumerable.Range(0, 5)
            .Select(i => AddExecution(jobId: jobId, startedOn: base_.AddMinutes(i)).Id)
            .ToList();

        // Skip the 3 most recent → should return the 2 oldest
        var oldest = _service.GetOldestExecutionsAfterCount(jobId, skipCount: 3).ToList();

        Assert.HasCount(2, oldest);
        // The returned records should be the two with the earliest timestamps
        var returnedIds = oldest.Select(e => e.Id).ToHashSet();
        Assert.Contains(ids[0], returnedIds);
        Assert.Contains(ids[1], returnedIds);
    }

    // --- GetExecutionsOlderThan ---

    [TestMethod]
    public void GetExecutionsOlderThanShouldReturnOnlyRecordsBeforeCutoff()
    {
        var cutoff = DateTimeOffset.UtcNow;
        AddExecution(startedOn: cutoff.AddMinutes(-5));
        AddExecution(startedOn: cutoff.AddMinutes(-1));
        AddExecution(startedOn: cutoff.AddMinutes(1));  // after cutoff, should be excluded

        var results = _service.GetExecutionsOlderThan(cutoff).ToList();

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(e => e.StartedOn < cutoff));
    }

    // --- CheckDatabaseConnectivity ---

    [TestMethod]
    public void CheckDatabaseConnectivityShouldReturnTrue()
    {
        Assert.IsTrue(_service.CheckDatabaseConnectivity());
    }

    // --- Helpers ---

    private ExecutionModel AddExecution(
        Guid? jobId = null,
        ExecutionStatus status = ExecutionStatus.CompletedAsSuccess,
        DateTimeOffset? startedOn = null)
    {
        var execution = MakeExecution(jobId, status, startedOn);
        _service.AddExecution(execution);
        return execution;
    }

    private static ExecutionModel MakeExecution(
        Guid? jobId = null,
        ExecutionStatus status = ExecutionStatus.CompletedAsSuccess,
        DateTimeOffset? startedOn = null) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId ?? Guid.NewGuid(),
        JobName = "Test Job",
        StartedOn = startedOn ?? DateTimeOffset.UtcNow,
        StartReason = ExecutionReason.Scheduled,
        Status = status,
        CompletedOn = DateTimeOffset.UtcNow,
        ExitCode = 0,
        StopReason = TerminationReason.Exited,
    };
}

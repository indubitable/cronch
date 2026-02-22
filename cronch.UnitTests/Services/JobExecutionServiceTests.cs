using cronch.Models;
using cronch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace cronch.UnitTests.Services;

[TestClass]
public class JobExecutionServiceTests
{
    // --- GetDefaultScriptLocation ---

    [TestMethod]
    public void GetDefaultScriptLocationShouldReturnTempPathWhenLocationIsNull()
    {
        var result = JobExecutionService.GetDefaultScriptLocation(new SettingsModel { DefaultScriptFileLocation = null });

        Assert.AreEqual(Path.GetTempPath(), result);
    }

    [TestMethod]
    public void GetDefaultScriptLocationShouldReturnTempPathWhenLocationIsEmpty()
    {
        var result = JobExecutionService.GetDefaultScriptLocation(new SettingsModel { DefaultScriptFileLocation = "" });

        Assert.AreEqual(Path.GetTempPath(), result);
    }

    [TestMethod]
    public void GetDefaultScriptLocationShouldReturnTempPathWhenLocationIsWhitespace()
    {
        var result = JobExecutionService.GetDefaultScriptLocation(new SettingsModel { DefaultScriptFileLocation = "   " });

        Assert.AreEqual(Path.GetTempPath(), result);
    }

    [TestMethod]
    public void GetDefaultScriptLocationShouldReturnConfiguredLocationWhenSet()
    {
        var result = JobExecutionService.GetDefaultScriptLocation(new SettingsModel { DefaultScriptFileLocation = "/scripts" });

        Assert.AreEqual("/scripts", result);
    }

    // --- GetRunEnvVars ---

    [TestMethod]
    public void GetRunEnvVarsShouldContainJobIdJobNameAndExecutionId()
    {
        var jobId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var job = new JobModel { Id = jobId, Name = "My Job" };
        var execution = new ExecutionModel { Id = executionId, JobId = jobId };

        var result = JobExecutionService.GetRunEnvVars(execution, job);

        Assert.AreEqual(jobId.ToString("D"), result["CRONCH_JOB_ID"]);
        Assert.AreEqual("My Job", result["CRONCH_JOB_NAME"]);
        Assert.AreEqual(executionId.ToString("D"), result["CRONCH_EXECUTION_ID"]);
    }

    // --- GetRunCompletionEnvVars ---

    [TestMethod]
    public void GetRunCompletionEnvVarsShouldContainAllRequiredFields()
    {
        var jobId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var startedOn = DateTimeOffset.UtcNow.AddMinutes(-1);
        var completedOn = DateTimeOffset.UtcNow;
        var job = new JobModel { Id = jobId, Name = "My Job" };
        var execution = new ExecutionModel
        {
            Id = executionId,
            JobId = jobId,
            StartedOn = startedOn,
            CompletedOn = completedOn,
            ExitCode = 0,
            Status = ExecutionStatus.CompletedAsSuccess,
            StopReason = TerminationReason.Exited,
        };

        var result = JobExecutionService.GetRunCompletionEnvVars(execution, job, "/output/file.txt");

        Assert.AreEqual(jobId.ToString("D"), result["CRONCH_JOB_ID"]);
        Assert.AreEqual("My Job", result["CRONCH_JOB_NAME"]);
        Assert.AreEqual(executionId.ToString("D"), result["CRONCH_EXECUTION_ID"]);
        Assert.AreEqual(startedOn.ToUnixTimeSeconds().ToString(), result["CRONCH_EXECUTION_STARTED_ON"]);
        Assert.AreEqual(completedOn.ToUnixTimeSeconds().ToString(), result["CRONCH_EXECUTION_COMPLETED_ON"]);
        Assert.AreEqual("0", result["CRONCH_EXECUTION_EXIT_CODE"]);
        Assert.AreEqual("CompletedAsSuccess", result["CRONCH_EXECUTION_STATUS"]);
        Assert.AreEqual("Exited", result["CRONCH_EXECUTION_STOP_REASON"]);
        Assert.AreEqual("/output/file.txt", result["CRONCH_EXECUTION_INTERNAL_OUTPUT_FILE"]);
    }

    [TestMethod]
    public void GetRunCompletionEnvVarsShouldUseEmptyStringForNullOptionalFields()
    {
        var execution = new ExecutionModel
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            StartedOn = DateTimeOffset.UtcNow,
            CompletedOn = null,
            ExitCode = null,
            Status = ExecutionStatus.Running,
            StopReason = null,
        };

        var result = JobExecutionService.GetRunCompletionEnvVars(execution, new JobModel(), "/output.txt");

        Assert.AreEqual(string.Empty, result["CRONCH_EXECUTION_COMPLETED_ON"]);
        Assert.AreEqual(string.Empty, result["CRONCH_EXECUTION_EXIT_CODE"]);
        Assert.AreEqual(string.Empty, result["CRONCH_EXECUTION_STOP_REASON"]);
    }

    // --- ExecuteRunCompletionScript ---

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenCompletionScriptIsEmpty()
    {
        var (sut, engine, _) = CreateSut();
        var settings = new SettingsModel { CompletionScriptExecutor = "bash", CompletionScript = "" };

        sut.ExecuteRunCompletionScript(MakeExecution(), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenCompletionScriptIsWhitespace()
    {
        var (sut, engine, _) = CreateSut();
        var settings = new SettingsModel { CompletionScriptExecutor = "bash", CompletionScript = "   " };

        sut.ExecuteRunCompletionScript(MakeExecution(), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenExecutorIsEmpty()
    {
        var (sut, engine, _) = CreateSut();
        var settings = new SettingsModel { CompletionScriptExecutor = "", CompletionScript = "echo done" };

        sut.ExecuteRunCompletionScript(MakeExecution(), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenStatusIsSuccessAndFlagIsFalse()
    {
        var (sut, engine, _) = CreateSut();
        var settings = ScriptSettings(runOnSuccess: false);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsSuccess), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldRunWhenStatusIsSuccessAndFlagIsTrue()
    {
        var (sut, engine, persistence) = CreateSut();
        var settings = ScriptSettings(runOnSuccess: true);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsSuccess), new JobModel(), settings, engine, persistence);

        engine.Received(1).PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenStatusIsErrorAndFlagIsFalse()
    {
        var (sut, engine, _) = CreateSut();
        var settings = ScriptSettings(runOnError: false);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsError), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldRunWhenStatusIsErrorAndFlagIsTrue()
    {
        var (sut, engine, persistence) = CreateSut();
        var settings = ScriptSettings(runOnError: true);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsError), new JobModel(), settings, engine, persistence);

        engine.Received(1).PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenStatusIsWarningAndFlagIsFalse()
    {
        var (sut, engine, _) = CreateSut();
        var settings = ScriptSettings(runOnWarning: false);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsWarning), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteRunCompletionScriptShouldSkipWhenStatusIsIndeterminateAndFlagIsFalse()
    {
        var (sut, engine, _) = CreateSut();
        var settings = ScriptSettings(runOnIndeterminate: false);

        sut.ExecuteRunCompletionScript(MakeExecution(ExecutionStatus.CompletedAsIndeterminate), new JobModel(), settings, engine, MakePersistence());

        engine.DidNotReceive().PerformExecution(Arg.Any<ExecutionModel>(), Arg.Any<JobModel>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<bool>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private static (JobExecutionService sut, ExecutionEngine engine, ExecutionPersistenceService persistence) CreateSut()
    {
        var engine = Substitute.For<ExecutionEngine>(Substitute.For<ILogger<ExecutionEngine>>());

        var persistence = MakePersistence();

        // SettingsService and IServiceProvider are not used by ExecuteRunCompletionScript
        var sut = new JobExecutionService(
            Substitute.For<ILogger<JobExecutionService>>(),
            null!,
            null!);

        return (sut, engine, persistence);
    }

    private static ExecutionPersistenceService MakePersistence()
    {
        var persistence = Substitute.For<ExecutionPersistenceService>(
            Substitute.For<ILogger<ExecutionPersistenceService>>(),
            Substitute.For<IConfiguration>());
        persistence.GetOutputPathName(Arg.Any<ExecutionModel>(), Arg.Any<bool>())
            .Returns(Path.Combine(Path.GetTempPath(), "cronch_test_output.txt"));
        return persistence;
    }

    private static ExecutionModel MakeExecution(ExecutionStatus status = ExecutionStatus.CompletedAsSuccess) => new()
    {
        Id = Guid.NewGuid(),
        JobId = Guid.NewGuid(),
        JobName = "Test Job",
        StartedOn = DateTimeOffset.UtcNow.AddSeconds(-5),
        CompletedOn = DateTimeOffset.UtcNow,
        ExitCode = 0,
        Status = status,
        StopReason = TerminationReason.Exited,
    };

    /// <summary>
    /// Creates a SettingsModel with a completion script configured.
    /// All run-on flags default to true unless explicitly set to false.
    /// </summary>
    private static SettingsModel ScriptSettings(
        bool runOnSuccess = true,
        bool runOnError = true,
        bool runOnWarning = true,
        bool runOnIndeterminate = true) => new()
    {
        CompletionScriptExecutor = "bash",
        CompletionScript = "echo done",
        RunCompletionScriptOnSuccess = runOnSuccess,
        RunCompletionScriptOnError = runOnError,
        RunCompletionScriptOnWarning = runOnWarning,
        RunCompletionScriptOnIndeterminate = runOnIndeterminate,
    };
}

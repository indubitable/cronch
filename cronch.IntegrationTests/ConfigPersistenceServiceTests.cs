using cronch.Models.Persistence;
using cronch.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace cronch.IntegrationTests;

[TestClass]
public class ConfigPersistenceServiceTests
{
    private string _tempDir = null!;
    private ConfigPersistenceService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = CreateService(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // --- Load ---

    [TestMethod]
    public void LoadShouldReturnNullWhenConfigFileDoesNotExist()
    {
        var result = _service.Load();

        Assert.IsNull(result);
    }

    // --- Save / Load round-trips ---

    [TestMethod]
    public void SaveAndLoadShouldRoundTripConfigFields()
    {
        var original = new ConfigPersistenceModel
        {
            MaxHistoryItemsShown = 100,
            DeleteHistoricalRunsAfterCount = 50,
            DeleteHistoricalRunsAfterDays = 30,
            DefaultScriptFileLocation = "/scripts",
            CompletionScriptExecutor = "bash",
            CompletionScriptExecutorArgs = "-c {0}",
            CompletionScript = "echo done",
            RunCompletionScriptOn = ["Success", "Error"],
        };

        _service.Save(original);
        var result = _service.Load();

        Assert.IsNotNull(result);
        Assert.AreEqual(original.MaxHistoryItemsShown, result.MaxHistoryItemsShown);
        Assert.AreEqual(original.DeleteHistoricalRunsAfterCount, result.DeleteHistoricalRunsAfterCount);
        Assert.AreEqual(original.DeleteHistoricalRunsAfterDays, result.DeleteHistoricalRunsAfterDays);
        Assert.AreEqual(original.DefaultScriptFileLocation, result.DefaultScriptFileLocation);
        Assert.AreEqual(original.CompletionScriptExecutor, result.CompletionScriptExecutor);
        Assert.AreEqual(original.CompletionScriptExecutorArgs, result.CompletionScriptExecutorArgs);
        Assert.AreEqual(original.CompletionScript, result.CompletionScript);
        Assert.HasCount(2, result.RunCompletionScriptOn);
        Assert.Contains("Success", result.RunCompletionScriptOn);
        Assert.Contains("Error", result.RunCompletionScriptOn);
    }

    [TestMethod]
    public void SaveAndLoadShouldRoundTripNullableFieldsAsNull()
    {
        var original = new ConfigPersistenceModel
        {
            MaxHistoryItemsShown = null,
            DeleteHistoricalRunsAfterCount = null,
            DeleteHistoricalRunsAfterDays = null,
            DefaultScriptFileLocation = null,
            CompletionScriptExecutor = null,
            CompletionScriptExecutorArgs = null,
            CompletionScript = null,
        };

        _service.Save(original);
        var result = _service.Load();

        Assert.IsNotNull(result);
        Assert.IsNull(result.MaxHistoryItemsShown);
        Assert.IsNull(result.DeleteHistoricalRunsAfterCount);
        Assert.IsNull(result.DeleteHistoricalRunsAfterDays);
        Assert.IsNull(result.DefaultScriptFileLocation);
        Assert.IsNull(result.CompletionScriptExecutor);
        Assert.IsNull(result.CompletionScriptExecutorArgs);
        Assert.IsNull(result.CompletionScript);
    }

    [TestMethod]
    public void SaveAndLoadShouldRoundTripAllJobFields()
    {
        var jobId = Guid.NewGuid();
        var original = new ConfigPersistenceModel
        {
            Jobs =
            [
                new JobPersistenceModel
                {
                    Id = jobId,
                    Name = "My Job",
                    Enabled = true,
                    CronSchedule = "0 * * * * *",
                    Executor = "bash",
                    ExecutorArgs = "-c {0}",
                    Script = "echo hello",
                    ScriptFilePathname = "/tmp/script.sh",
                    TimeLimitSecs = 30.5,
                    Parallelism = 2,
                    MarkParallelSkipAs = "Warning",
                    Keywords = ["ERROR", "WARN"],
                    StdOutProcessing = "WarningOnMatchingKeywords",
                    StdErrProcessing = "ErrorOnAnyOutput",
                }
            ]
        };

        _service.Save(original);
        var result = _service.Load();

        Assert.IsNotNull(result);
        Assert.HasCount(1, result.Jobs);
        var job = result.Jobs[0];
        Assert.AreEqual(jobId, job.Id);
        Assert.AreEqual("My Job", job.Name);
        Assert.IsTrue(job.Enabled);
        Assert.AreEqual("0 * * * * *", job.CronSchedule);
        Assert.AreEqual("bash", job.Executor);
        Assert.AreEqual("-c {0}", job.ExecutorArgs);
        Assert.AreEqual("echo hello", job.Script);
        Assert.AreEqual("/tmp/script.sh", job.ScriptFilePathname);
        Assert.AreEqual(30.5, job.TimeLimitSecs);
        Assert.AreEqual(2, job.Parallelism);
        Assert.AreEqual("Warning", job.MarkParallelSkipAs);
        Assert.HasCount(2, job.Keywords);
        Assert.Contains("ERROR", job.Keywords);
        Assert.Contains("WARN", job.Keywords);
        Assert.AreEqual("WarningOnMatchingKeywords", job.StdOutProcessing);
        Assert.AreEqual("ErrorOnAnyOutput", job.StdErrProcessing);
    }

    [TestMethod]
    public void SaveAndLoadShouldRoundTripJobNullableFieldsAsNull()
    {
        var original = new ConfigPersistenceModel
        {
            Jobs =
            [
                new JobPersistenceModel
                {
                    Id = Guid.NewGuid(),
                    ExecutorArgs = null,
                    ScriptFilePathname = null,
                    TimeLimitSecs = null,
                    Parallelism = null,
                }
            ]
        };

        _service.Save(original);
        var result = _service.Load();

        Assert.IsNotNull(result);
        var job = result.Jobs[0];
        Assert.IsNull(job.ExecutorArgs);
        Assert.IsNull(job.ScriptFilePathname);
        Assert.IsNull(job.TimeLimitSecs);
        Assert.IsNull(job.Parallelism);
    }

    // --- Save behaviour ---

    [TestMethod]
    public void SaveShouldSortJobsByIdBeforeWriting()
    {
        var id1 = Guid.Parse("11111111-0000-0000-0000-000000000000");
        var id2 = Guid.Parse("22222222-0000-0000-0000-000000000000");
        var id3 = Guid.Parse("33333333-0000-0000-0000-000000000000");

        // Deliberately insert in reverse order
        _service.Save(new ConfigPersistenceModel
        {
            Jobs =
            [
                new JobPersistenceModel { Id = id3, Name = "C" },
                new JobPersistenceModel { Id = id1, Name = "A" },
                new JobPersistenceModel { Id = id2, Name = "B" },
            ]
        });
        var result = _service.Load();

        Assert.IsNotNull(result);
        Assert.HasCount(3, result.Jobs);
        Assert.AreEqual(id1, result.Jobs[0].Id);
        Assert.AreEqual(id2, result.Jobs[1].Id);
        Assert.AreEqual(id3, result.Jobs[2].Id);
    }

    [TestMethod]
    public void SaveShouldCreateDirectoryIfItDoesNotExist()
    {
        var nestedDir = Path.Combine(_tempDir, "subdir", "nested");
        var service = CreateService(nestedDir);

        service.Save(new ConfigPersistenceModel());

        Assert.IsTrue(File.Exists(Path.Combine(nestedDir, "config.xml")));
    }

    [TestMethod]
    public void SaveShouldOverwritePreviousConfig()
    {
        _service.Save(new ConfigPersistenceModel { MaxHistoryItemsShown = 10 });
        _service.Save(new ConfigPersistenceModel { MaxHistoryItemsShown = 99 });

        var result = _service.Load();

        Assert.IsNotNull(result);
        Assert.AreEqual(99, result.MaxHistoryItemsShown);
    }

    // --- Helpers ---

    private static ConfigPersistenceService CreateService(string configLocation)
    {
        var config = Substitute.For<IConfiguration>();
        config["ConfigLocation"].Returns(configLocation);
        return new ConfigPersistenceService(NullLogger<ConfigPersistenceService>.Instance, config);
    }
}

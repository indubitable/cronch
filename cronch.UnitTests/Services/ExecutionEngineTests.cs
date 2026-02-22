using cronch.Models;
using cronch.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Diagnostics;
using System.Reflection;

namespace cronch.UnitTests.Services;

[TestClass]
public class ExecutionEngineTests
{
    // --- ProcessLineForStatusUpdate: OutputProcessing.None ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenDirectiveIsNone()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.None, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.WarningOnAnyOutput ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetWarningWhenDirectiveIsWarningOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.WarningOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsWarning, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotDowngradeFromErrorWhenDirectiveIsWarningOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsError;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.WarningOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.ErrorOnAnyOutput ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetErrorWhenDirectiveIsErrorOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.ErrorOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldUpgradeFromWarningToErrorWhenDirectiveIsErrorOnAnyOutput()
    {
        var status = ExecutionStatus.CompletedAsWarning;

        ExecutionEngine.ProcessLineForStatusUpdate("some output", [], JobModel.OutputProcessing.ErrorOnAnyOutput, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.WarningOnMatchingKeywords ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetWarningWhenKeywordMatchesAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsWarning, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenNoKeywordMatchAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("normal output", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotDowngradeFromErrorWhenKeywordMatchesAndDirectiveIsWarningOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsError;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.WarningOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    // --- ProcessLineForStatusUpdate: OutputProcessing.ErrorOnMatchingKeywords ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldSetErrorWhenKeywordMatchesAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldUpgradeFromWarningToErrorWhenKeywordMatchesAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsWarning;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR keyword", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotChangeStatusWhenNoKeywordMatchAndDirectiveIsErrorOnMatchingKeywords()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("normal output", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    // --- ProcessLineForStatusUpdate: keyword matching behavior ---

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldMatchKeywordsCaseSensitively()
    {
        var statusLower = ExecutionStatus.CompletedAsSuccess;
        var statusUpper = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains error", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref statusLower);
        ExecutionEngine.ProcessLineForStatusUpdate("output contains ERROR", ["ERROR"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref statusUpper);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, statusLower, "Lowercase 'error' should not match keyword 'ERROR'");
        Assert.AreEqual(ExecutionStatus.CompletedAsError, statusUpper, "Uppercase 'ERROR' should match keyword 'ERROR'");
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldMatchWhenAnyKeywordInListMatches()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("output contains WARN", ["ERROR", "WARN", "FATAL"], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, status);
    }

    [TestMethod]
    public void ProcessLineForStatusUpdateShouldNotMatchWhenKeywordListIsEmpty()
    {
        var status = ExecutionStatus.CompletedAsSuccess;

        ExecutionEngine.ProcessLineForStatusUpdate("ERROR WARN FATAL", [], JobModel.OutputProcessing.ErrorOnMatchingKeywords, ref status);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, status);
    }

    // --- PerformExecution: exit codes and basic status ---

    [TestMethod]
    public void PerformExecutionShouldCompleteSuccessfullyOnExitCodeZero()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, execution.Status);
        Assert.AreEqual(TerminationReason.Exited, execution.StopReason);
        Assert.AreEqual(0, execution.ExitCode);
        Assert.IsNotNull(execution.CompletedOn);
    }

    [TestMethod]
    public void PerformExecutionShouldSetErrorStatusOnNonZeroExitCode()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(1);
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, execution.Status);
        Assert.AreEqual(1, execution.ExitCode);
    }

    // --- PerformExecution: time limit ---

    [TestMethod]
    public void PerformExecutionShouldCompleteSuccessfullyWhenExitingWithinTimeLimit()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.WaitForExit(Arg.Any<TimeSpan>()).Returns(true);
        mockProcess.GetExitCode().Returns(0);
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(timeLimitSecs: 5), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual(ExecutionStatus.CompletedAsSuccess, execution.Status);
        Assert.AreEqual(TerminationReason.Exited, execution.StopReason);
    }

    [TestMethod]
    public void PerformExecutionShouldSetTimedOutStatusWhenProcessExceedsTimeLimit()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.WaitForExit(Arg.Any<TimeSpan>()).Returns(false, true); // timeout, then kill-wait succeeds
        mockProcess.GetExitCode().Returns(0);
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(timeLimitSecs: 5), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual(ExecutionStatus.CompletedAsError, execution.Status);
        Assert.AreEqual(TerminationReason.TimedOut, execution.StopReason);
        mockProcess.Received(1).Kill();
    }

    [TestMethod]
    public void PerformExecutionShouldSetIndeterminateStatusWhenKillAfterTimeoutDoesNotComplete()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.WaitForExit(Arg.Any<TimeSpan>()).Returns(false); // always fails
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(timeLimitSecs: 5), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual(ExecutionStatus.CompletedAsIndeterminate, execution.Status);
    }

    // --- PerformExecution: cancellation ---

    [TestMethod]
    public void PerformExecutionShouldSetUserTriggeredStopReasonWhenCancelled()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        var cancelSource = new CancellationTokenSource();
        mockProcess.When(p => p.WaitForExit()).Do(_ => cancelSource.Cancel());
        mockProcess.GetExitCode().Returns(0);
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(), "/tmp/test.sh", new MemoryStream(), false, [], cancelSource.Token);

        Assert.AreEqual(TerminationReason.UserTriggered, execution.StopReason);
        mockProcess.Received().Kill();
    }

    // --- PerformExecution: executor args formatting ---

    [TestMethod]
    public void PerformExecutionShouldSubstituteScriptPathInExecutorArgsWhenPlaceholderPresent()
    {
        var (engine, mockProcess, _, mockFactory) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        ProcessStartInfo? capturedStartInfo = null;
        mockFactory.Create(Arg.Do<ProcessStartInfo>(si => capturedStartInfo = si)).Returns(mockProcess);

        engine.PerformExecution(MakeExecution(), MakeJob(executorArgs: "-c {0}"), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual("-c /tmp/test.sh", capturedStartInfo!.Arguments);
    }

    [TestMethod]
    public void PerformExecutionShouldAppendScriptPathToExecutorArgsWhenNoPlaceholder()
    {
        var (engine, mockProcess, _, mockFactory) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        ProcessStartInfo? capturedStartInfo = null;
        mockFactory.Create(Arg.Do<ProcessStartInfo>(si => capturedStartInfo = si)).Returns(mockProcess);

        engine.PerformExecution(MakeExecution(), MakeJob(executorArgs: "--login"), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual("--login /tmp/test.sh", capturedStartInfo!.Arguments);
    }

    [TestMethod]
    public void PerformExecutionShouldUseScriptPathAsOnlyArgWhenExecutorArgsAreEmpty()
    {
        var (engine, mockProcess, _, mockFactory) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        ProcessStartInfo? capturedStartInfo = null;
        mockFactory.Create(Arg.Do<ProcessStartInfo>(si => capturedStartInfo = si)).Returns(mockProcess);

        engine.PerformExecution(MakeExecution(), MakeJob(executorArgs: null), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        Assert.AreEqual("/tmp/test.sh", capturedStartInfo!.Arguments);
    }

    // --- PerformExecution: script file management ---

    [TestMethod]
    public void PerformExecutionShouldWriteJobScriptToTargetFile()
    {
        var (engine, mockProcess, mockFileAccess, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);

        engine.PerformExecution(MakeExecution(), MakeJob(script: "echo hello"), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        mockFileAccess.Received(1).WriteAllText("/tmp/test.sh", "echo hello");
    }

    [TestMethod]
    public void PerformExecutionShouldDeleteScriptFileOnSuccess()
    {
        var (engine, mockProcess, mockFileAccess, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);

        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        mockFileAccess.Received(1).Delete("/tmp/test.sh");
    }

    [TestMethod]
    public void PerformExecutionShouldDeleteScriptFileEvenWhenExceptionOccurs()
    {
        var (engine, mockProcess, mockFileAccess, _) = CreateSut();
        mockProcess.When(p => p.Start()).Do(_ => throw new InvalidOperationException("process failed to start"));
        var execution = MakeExecution();

        engine.PerformExecution(execution, MakeJob(), "/tmp/test.sh", new MemoryStream(), false, [], CancellationToken.None);

        mockFileAccess.Received(1).Delete("/tmp/test.sh");
        Assert.AreEqual(ExecutionStatus.CompletedAsIndeterminate, execution.Status);
    }

    // --- PerformExecution: environment variables ---

    [TestMethod]
    public void PerformExecutionShouldPassEnvironmentVariablesToProcess()
    {
        var (engine, mockProcess, _, mockFactory) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        ProcessStartInfo? capturedStartInfo = null;
        mockFactory.Create(Arg.Do<ProcessStartInfo>(si => capturedStartInfo = si)).Returns(mockProcess);

        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", new MemoryStream(), false, new Dictionary<string, string> { { "MY_VAR", "my_value" } }, CancellationToken.None);

        Assert.AreEqual("my_value", capturedStartInfo!.EnvironmentVariables["MY_VAR"]);
    }

    // --- PerformExecution: output formatting ---

    [TestMethod]
    public void PerformExecutionShouldWriteRawOutputWhenFormatOutputIsFalse()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        mockProcess.When(p => p.WaitForExit()).Do(_ =>
            mockProcess.OutputDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs("hello world")));

        using var outputStream = new MemoryStream();
        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", outputStream, false, [], CancellationToken.None);

        outputStream.Position = 0;
        var output = new StreamReader(outputStream).ReadToEnd();
        Assert.Contains("hello world", output);
        Assert.DoesNotStartWith("O ", output.TrimStart(), "Output should not have 'O ' prefix when formatOutput is false");
    }

    [TestMethod]
    public void PerformExecutionShouldWriteFormattedPrefixForStdoutWhenFormatOutputIsTrue()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        mockProcess.When(p => p.WaitForExit()).Do(_ =>
            mockProcess.OutputDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs("hello world")));

        using var outputStream = new MemoryStream();
        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", outputStream, true, [], CancellationToken.None);

        outputStream.Position = 0;
        var output = new StreamReader(outputStream).ReadToEnd();
        Assert.StartsWith("O ", output.TrimStart(), $"Expected 'O ' prefix, got: {output}");
        Assert.Contains("hello world", output);
    }

    [TestMethod]
    public void PerformExecutionShouldWriteFormattedPrefixForStderrWhenFormatOutputIsTrue()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        mockProcess.When(p => p.WaitForExit()).Do(_ =>
            mockProcess.ErrorDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs("error line")));

        using var outputStream = new MemoryStream();
        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", outputStream, true, [], CancellationToken.None);

        outputStream.Position = 0;
        var output = new StreamReader(outputStream).ReadToEnd();
        Assert.StartsWith("E ", output.TrimStart(), $"Expected 'E ' prefix, got: {output}");
        Assert.Contains("error line", output);
    }

    [TestMethod]
    public void PerformExecutionShouldIgnoreNullAndWhitespaceOutputLines()
    {
        var (engine, mockProcess, _, _) = CreateSut();
        mockProcess.GetExitCode().Returns(0);
        mockProcess.When(p => p.WaitForExit()).Do(_ =>
        {
            mockProcess.OutputDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs(null));
            mockProcess.OutputDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs("   "));
            mockProcess.OutputDataReceived += Raise.Event<DataReceivedEventHandler>(new object(), MakeDataArgs("actual output"));
        });

        using var outputStream = new MemoryStream();
        engine.PerformExecution(MakeExecution(), MakeJob(), "/tmp/test.sh", outputStream, false, [], CancellationToken.None);

        outputStream.Position = 0;
        var output = new StreamReader(outputStream).ReadToEnd().Trim();
        Assert.AreEqual("actual output", output);
    }

    // --- Helpers ---

    private static (ExecutionEngine engine, ProcessWrapper mockProcess, FileAccessWrapper mockFileAccess, ProcessFactory mockFactory) CreateSut()
    {
        var mockFileAccess = Substitute.For<FileAccessWrapper>();
        var mockProcess = Substitute.For<ProcessWrapper>();
        var mockFactory = Substitute.For<ProcessFactory>();
        mockFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(mockProcess);
        var engine = new ExecutionEngine(
            Substitute.For<ILogger<ExecutionEngine>>(),
            mockFileAccess,
            mockFactory);
        return (engine, mockProcess, mockFileAccess, mockFactory);
    }

    private static ExecutionModel MakeExecution() => new()
    {
        Id = Guid.NewGuid(),
        JobId = Guid.NewGuid(),
        JobName = "Test Job",
        StartedOn = DateTimeOffset.UtcNow,
        Status = ExecutionStatus.Running,
    };

    private static JobModel MakeJob(
        string executor = "executor",
        string? executorArgs = null,
        string script = "echo hi",
        int? timeLimitSecs = null) => new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Job",
            Executor = executor,
            ExecutorArgs = executorArgs,
            Script = script,
            TimeLimitSecs = timeLimitSecs,
        };

    // DataReceivedEventArgs has an internal constructor; create it via reflection.
    private static DataReceivedEventArgs MakeDataArgs(string? data) =>
        (DataReceivedEventArgs)typeof(DataReceivedEventArgs)
            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string)], null)!
            .Invoke([data]);
}

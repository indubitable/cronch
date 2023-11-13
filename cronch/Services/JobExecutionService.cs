using cronch.Models;
using cronch.Models.Persistence;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace cronch.Services;

public class JobExecutionService
{
    public enum ExecutionReason
    {
        Scheduled = 0,
        Manual = 1,
    }

    public enum ExecutionStatus
    {
        Unknown,
        Running,
        CompletedAsSuccess,
        CompletedAsIndeterminate,
        CompletedAsWarning,
        CompletedAsError,
    }

    public enum TerminationReason
    {
        NoneSpecified,
        Exited,
        TimedOut,
        SkippedForParallelism
    }

    public readonly record struct ExecutionIdentifier(Guid JobId, DateTimeOffset StartedOn, string RandomComponent);

    private readonly ILogger<JobExecutionService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<ExecutionIdentifier, Thread> _executions = new();

    public JobExecutionService(ILogger<JobExecutionService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public virtual void ExecuteJob(JobModel jobModel, ExecutionReason reason)
    {
        var execution = ExecutionPersistenceModel.CreateNew(jobModel.Id, reason.ToString(), ExecutionStatus.Unknown.ToString());

        // Check for parallelism limits
        if (jobModel.Parallelism.HasValue)
        {
            var runningJobCount = _executions.Where(kvp => kvp.Key.JobId == jobModel.Id).Count();
            if (runningJobCount >= jobModel.Parallelism.Value)
            {
                // Execution cannot proceed

                _logger.LogInformation("Parallelism limit exceeded for job '{Name}' ({Id}). Skipping execution.", jobModel.Name, jobModel.Id);

                var persistence = _serviceProvider.GetRequiredService<JobPersistenceService>();

                execution.CompletedOn = execution.StartedOn;
                execution.TerminationReason = TerminationReason.SkippedForParallelism.ToString();

                try
                {
                    switch (jobModel.MarkParallelSkipAs)
                    {
                        case JobModel.ParallelSkipProcessing.MarkAsIndeterminate:
                            execution.ExecutionStatus = ExecutionStatus.CompletedAsIndeterminate.ToString();
                            persistence.SaveExecution(execution);
                            persistence.SaveLatestExecutionForJob(execution, false);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsWarning:
                            execution.ExecutionStatus = ExecutionStatus.CompletedAsWarning.ToString();
                            persistence.SaveExecution(execution);
                            persistence.SaveLatestExecutionForJob(execution, false);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsError:
                            execution.ExecutionStatus = ExecutionStatus.CompletedAsError.ToString();
                            persistence.SaveExecution(execution);
                            persistence.SaveLatestExecutionForJob(execution, false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to save execution state for job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());
                }

                // Leave and never look back
                return;
            }
        }

        var thread = new Thread(() => PerformExecution(execution, jobModel))
        {
            IsBackground = true
        };

        // Register the execution
        if (!_executions.TryAdd(GetExecutionIdentifierFromModel(execution), thread))
        {
            throw new InvalidOperationException("Unable to execute job: could not register execution");
        }

        thread.Start();
    }

    private void PerformExecution(ExecutionPersistenceModel execution, JobModel jobModel)
    {
        var persistence = _serviceProvider.GetRequiredService<JobPersistenceService>();

        var tempDir = Path.GetTempPath();
        var scriptFile = Path.Combine(tempDir, Path.GetRandomFileName());
        if (!string.IsNullOrWhiteSpace(jobModel.ScriptFilePathname))
        {
            scriptFile = Path.Combine(tempDir, jobModel.ScriptFilePathname.Trim());
        }

        var intermediateExecutionStatus = ExecutionStatus.CompletedAsSuccess;

        try
        {
            execution.ExecutionStatus = ExecutionStatus.Running.ToString();
            persistence.SaveExecution(execution);
            persistence.SaveLatestExecutionForJob(execution, true);

            using var stdoutStream = File.Open(persistence.GetExecutionPathName(execution, "out.txt", false), FileMode.Create, FileAccess.Write, FileShare.Read);
            using var stdoutWriter = new StreamWriter(stdoutStream, leaveOpen: true);
            using var stderrStream = File.Open(persistence.GetExecutionPathName(execution, "err.txt", false), FileMode.Create, FileAccess.Write, FileShare.Read);
            using var stderrWriter = new StreamWriter(stderrStream, leaveOpen: true);

            File.WriteAllText(scriptFile, jobModel.Script);

            var executorFile = jobModel.Executor;
            var executorArgs = (jobModel.ExecutorArgs ?? "").Trim();
            if (executorArgs.Contains("{0}"))
            {
                executorArgs = string.Format(executorArgs, scriptFile);
            }
            else
            {
                executorArgs += $" {scriptFile}";
                executorArgs = executorArgs.Trim();
            }
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                RedirectStandardOutput = true,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                RedirectStandardError = true,
                CreateNoWindow = true,
                FileName = executorFile,
                Arguments = executorArgs
            };
            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    stdoutWriter.WriteLine($"{DateTimeOffset.UtcNow:yyyy-MM-dd_HH:mm:ss} {line}");
                    ProcessLineForStatusUpdate(line, jobModel.Keywords, jobModel.StdOutProcessing, ref intermediateExecutionStatus);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    stderrWriter.WriteLine($"{DateTimeOffset.UtcNow:yyyy-MM-dd_HH:mm:ss} {line}");
                    ProcessLineForStatusUpdate(line, jobModel.Keywords, jobModel.StdErrProcessing, ref intermediateExecutionStatus);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (jobModel.TimeLimitSecs.HasValue)
            {
                if (process.WaitForExit(TimeSpan.FromSeconds(jobModel.TimeLimitSecs.Value)))
                {
                    execution.TerminationReason = TerminationReason.Exited.ToString();
                }
                else
                {
                    execution.TerminationReason = TerminationReason.TimedOut.ToString();
                    execution.ExecutionStatus = ExecutionStatus.CompletedAsError.ToString();
                    persistence.SaveExecution(execution);
                    process.Kill(true);
                    if (!process.WaitForExit(800))
                    {
                        _logger.LogWarning("Job '{Name}' ({Id}), execution {ExecName}, timed out and could not be fully killed in a reasonable amount of time", jobModel.Name, jobModel.Id, execution.GetExecutionName());
                        throw new TimeoutException();
                    }
                }
            }
            else
            {
                process.WaitForExit();
                execution.TerminationReason = TerminationReason.Exited.ToString();
            }

            execution.CompletedOn = DateTimeOffset.UtcNow;
            execution.ExitCode = process.ExitCode;

            if (execution.ExecutionStatus.Equals(ExecutionStatus.Running.ToString()))
            {
                if (execution.ExitCode != 0)
                {
                    // Always mark as error when the exit code is non-zero
                    execution.ExecutionStatus = ExecutionStatus.CompletedAsError.ToString();
                }
                else
                {
                    execution.ExecutionStatus = intermediateExecutionStatus.ToString();
                }
            }

            persistence.SaveExecution(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());
        }
        finally
        {
            try
            {
                File.Delete(scriptFile);
            }
            catch (Exception)
            {
                // Ignore.
            }

            _executions.TryRemove(GetExecutionIdentifierFromModel(execution), out var _);
        }
    }

    private static void ProcessLineForStatusUpdate(string line, List<string> keywords, JobModel.OutputProcessing processingDirective, ref ExecutionStatus intermediateExecutionStatus)
    {
        var keywordMatchExists = false;
        if (processingDirective == JobModel.OutputProcessing.WarningOnMatchingKeywords || processingDirective == JobModel.OutputProcessing.ErrorOnMatchingKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (line.Contains(keyword, StringComparison.InvariantCulture))
                {
                    keywordMatchExists = true;
                    break;
                }
            }
        }

        switch (processingDirective)
        {
            case JobModel.OutputProcessing.WarningOnAnyOutput:
                if (intermediateExecutionStatus != ExecutionStatus.CompletedAsError)
                {
                    intermediateExecutionStatus = ExecutionStatus.CompletedAsWarning;
                }
                break;
            case JobModel.OutputProcessing.ErrorOnAnyOutput:
                intermediateExecutionStatus = ExecutionStatus.CompletedAsError;
                break;
            case JobModel.OutputProcessing.WarningOnMatchingKeywords:
                if (intermediateExecutionStatus != ExecutionStatus.CompletedAsError && keywordMatchExists)
                {
                    intermediateExecutionStatus = ExecutionStatus.CompletedAsWarning;
                }
                break;
            case JobModel.OutputProcessing.ErrorOnMatchingKeywords:
                if (keywordMatchExists)
                {
                    intermediateExecutionStatus = ExecutionStatus.CompletedAsError;
                }
                break;
        }
    }

    private static ExecutionIdentifier GetExecutionIdentifierFromModel(ExecutionPersistenceModel execution)
    {
        return new ExecutionIdentifier(execution.JobId, execution.StartedOn, execution.RandomComponent);
    }
}

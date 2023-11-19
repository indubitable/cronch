using cronch.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace cronch.Services;

public class JobExecutionService(ILogger<JobExecutionService> _logger, IServiceProvider _serviceProvider)
{
    public readonly record struct ExecutionIdentifier(Guid JobId, Guid ExecutionId, DateTimeOffset StartedOn);

    private readonly ConcurrentDictionary<ExecutionIdentifier, Thread> _executions = new();

    public virtual DateTimeOffset? GetLatestExecutionForJob(Guid jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
        return executionPersistenceService.GetLatestExecutionForJob(jobId);
    }

    public virtual ExecutionPersistenceService.ExecutionStatistics GetExecutionStatistics(DateTimeOffset from, DateTimeOffset to)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
        return executionPersistenceService.GetExecutionStatistics(from, to);
    }

    public virtual List<ExecutionIdentifier> GetCurrentExecutions()
    {
        return _executions.Keys.ToList();
    }

    public virtual void ExecuteJob(JobModel jobModel, ExecutionModel.ExecutionReason reason)
    {
        var execution = ExecutionModel.CreateNew(jobModel.Id, reason, ExecutionModel.ExecutionStatus.Unknown);

        // Check for parallelism limits
        if (jobModel.Parallelism.HasValue)
        {
            var runningJobCount = _executions.Where(kvp => kvp.Key.JobId == jobModel.Id).Count();
            if (runningJobCount >= jobModel.Parallelism.Value)
            {
                // Execution cannot proceed

                _logger.LogInformation("Parallelism limit exceeded for job '{Name}' ({Id}). Skipping execution.", jobModel.Name, jobModel.Id);

                execution.CompletedOn = execution.StartedOn;
                execution.StopReason = ExecutionModel.TerminationReason.SkippedForParallelism;

                using var scope = _serviceProvider.CreateScope();
                var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

                try
                {
                    switch (jobModel.MarkParallelSkipAs)
                    {
                        case JobModel.ParallelSkipProcessing.MarkAsIndeterminate:
                            execution.Status = ExecutionModel.ExecutionStatus.CompletedAsIndeterminate;
                            executionPersistenceService.AddExecution(execution);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsWarning:
                            execution.Status = ExecutionModel.ExecutionStatus.CompletedAsWarning;
                            executionPersistenceService.AddExecution(execution);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsError:
                            execution.Status = ExecutionModel.ExecutionStatus.CompletedAsError;
                            executionPersistenceService.AddExecution(execution);
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

    private void PerformExecution(ExecutionModel execution, JobModel jobModel)
    {
        using var scope = _serviceProvider.CreateScope();
        var persistence = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        var tempDir = Path.GetTempPath();
        var scriptFile = Path.Combine(tempDir, Path.GetRandomFileName());
        if (!string.IsNullOrWhiteSpace(jobModel.ScriptFilePathname))
        {
            scriptFile = Path.Combine(tempDir, jobModel.ScriptFilePathname.Trim());
        }

        var intermediateExecutionStatus = ExecutionModel.ExecutionStatus.CompletedAsSuccess;

        try
        {
            execution.Status = ExecutionModel.ExecutionStatus.Running;
            persistence.AddExecution(execution);
            persistence.GetExecutionPathName(execution, "", true);

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
                    execution.StopReason = ExecutionModel.TerminationReason.Exited;
                }
                else
                {
                    execution.StopReason = ExecutionModel.TerminationReason.TimedOut;
                    execution.Status = ExecutionModel.ExecutionStatus.CompletedAsError;
                    persistence.UpdateExecution(execution);
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
                execution.StopReason = ExecutionModel.TerminationReason.Exited;
            }

            execution.CompletedOn = DateTimeOffset.UtcNow;
            execution.ExitCode = process.ExitCode;

            if (execution.Status == ExecutionModel.ExecutionStatus.Running)
            {
                if (execution.ExitCode != 0)
                {
                    // Always mark as error when the exit code is non-zero
                    execution.Status = ExecutionModel.ExecutionStatus.CompletedAsError;
                }
                else
                {
                    execution.Status = intermediateExecutionStatus;
                }
            }

            persistence.UpdateExecution(execution);
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

    private static void ProcessLineForStatusUpdate(string line, List<string> keywords, JobModel.OutputProcessing processingDirective, ref ExecutionModel.ExecutionStatus intermediateExecutionStatus)
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
                if (intermediateExecutionStatus != ExecutionModel.ExecutionStatus.CompletedAsError)
                {
                    intermediateExecutionStatus = ExecutionModel.ExecutionStatus.CompletedAsWarning;
                }
                break;
            case JobModel.OutputProcessing.ErrorOnAnyOutput:
                intermediateExecutionStatus = ExecutionModel.ExecutionStatus.CompletedAsError;
                break;
            case JobModel.OutputProcessing.WarningOnMatchingKeywords:
                if (intermediateExecutionStatus != ExecutionModel.ExecutionStatus.CompletedAsError && keywordMatchExists)
                {
                    intermediateExecutionStatus = ExecutionModel.ExecutionStatus.CompletedAsWarning;
                }
                break;
            case JobModel.OutputProcessing.ErrorOnMatchingKeywords:
                if (keywordMatchExists)
                {
                    intermediateExecutionStatus = ExecutionModel.ExecutionStatus.CompletedAsError;
                }
                break;
        }
    }

    private static ExecutionIdentifier GetExecutionIdentifierFromModel(ExecutionModel execution)
    {
        return new ExecutionIdentifier(execution.JobId, execution.Id, execution.StartedOn);
    }
}

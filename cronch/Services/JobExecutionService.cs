using cronch.Models;
using cronch.Models.ViewModels;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace cronch.Services;

public class JobExecutionService(ILogger<JobExecutionService> _logger, IServiceProvider _serviceProvider)
{
    public readonly record struct ExecutionIdentifier(Guid JobId, Guid ExecutionId, DateTimeOffset StartedOn);

    private readonly ConcurrentDictionary<ExecutionIdentifier, Thread> _executions = new();

    public virtual Dictionary<Guid, DateTimeOffset> GetLatestExecutions()
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
        return executionPersistenceService.GetLatestExecutions();
    }

    public virtual ExecutionPersistenceService.ExecutionStatistics GetExecutionStatistics(DateTimeOffset from, DateTimeOffset to)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();
        return executionPersistenceService.GetExecutionStatistics(from, to);
    }

    public virtual ExecutionViewModel GetExecution(Guid executionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        return ConvertExecutionModelToViewModel(executionPersistenceService.GetExecution(executionId));
    }

    public virtual string GetOutputForExecution(Guid executionId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        var execution = executionPersistenceService.GetExecution(executionId);
        var outputPathname = executionPersistenceService.GetOutputPathName(execution, false);

        var outputContents = string.Empty;
        try
        {
            if (File.Exists(outputPathname)) outputContents = File.ReadAllText(outputPathname);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read stdout/stderr for execution {Id}", executionId);
        }
        return outputContents;
    }

    public virtual List<ExecutionViewModel> GetRecentExecutions(int maxCount, Guid? jobId)
    {
        using var scope = _serviceProvider.CreateScope();
        var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

        return executionPersistenceService.GetRecentExecutions(maxCount, jobId)
            .Select(ConvertExecutionModelToViewModel)
            .ToList();
    }

    public virtual List<ExecutionIdentifier> GetAllCurrentExecutions()
    {
        return _executions.Keys.ToList();
    }

    public virtual void ExecuteJob(JobModel jobModel, ExecutionReason reason)
    {
        var execution = ExecutionModel.CreateNew(jobModel.Id, jobModel.Name, reason, ExecutionStatus.Unknown);

        // Check for parallelism limits
        if (jobModel.Parallelism.HasValue)
        {
            var runningJobCount = _executions.Where(kvp => kvp.Key.JobId == jobModel.Id).Count();
            if (runningJobCount >= jobModel.Parallelism.Value)
            {
                // Execution cannot proceed

                _logger.LogInformation("Parallelism limit exceeded for job '{Name}' ({Id}). Skipping execution.", jobModel.Name, jobModel.Id);

                execution.CompletedOn = execution.StartedOn;
                execution.StopReason = TerminationReason.SkippedForParallelism;

                using var scope = _serviceProvider.CreateScope();
                var executionPersistenceService = scope.ServiceProvider.GetRequiredService<ExecutionPersistenceService>();

                try
                {
                    switch (jobModel.MarkParallelSkipAs)
                    {
                        case JobModel.ParallelSkipProcessing.MarkAsIndeterminate:
                            execution.Status = ExecutionStatus.CompletedAsIndeterminate;
                            executionPersistenceService.AddExecution(execution);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsWarning:
                            execution.Status = ExecutionStatus.CompletedAsWarning;
                            executionPersistenceService.AddExecution(execution);
                            break;
                        case JobModel.ParallelSkipProcessing.MarkAsError:
                            execution.Status = ExecutionStatus.CompletedAsError;
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

    private ExecutionViewModel ConvertExecutionModelToViewModel(ExecutionModel model)
    {
        var currentExecutions = _executions.Keys.ToList();
        ExecutionStatus fixOutdatedRunningStatus(Guid execId, ExecutionStatus es) => (es == ExecutionStatus.Running && !currentExecutions.Any(ce => ce.ExecutionId == execId)) ? ExecutionStatus.Unknown : es;

        return new ExecutionViewModel
        {
            JobId = model.JobId,
            ExecutionId = model.Id,
            JobName = model.JobName,
            StartedOn = model.StartedOn,
            CompletedOn = model.CompletedOn,
            Status = fixOutdatedRunningStatus(model.Id, model.Status),
            StartReason = model.StartReason,
            StopReason = model.StopReason,
        };
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

        var intermediateExecutionStatus = ExecutionStatus.CompletedAsSuccess;

        try
        {
            execution.Status = ExecutionStatus.Running;
            persistence.AddExecution(execution);

            using var outputStream = File.Open(persistence.GetOutputPathName(execution, true), FileMode.Create, FileAccess.Write, FileShare.Read);
            using var outputWriter = new StreamWriter(outputStream, leaveOpen: true);

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
                    outputWriter.WriteLine($"O {DateTimeOffset.UtcNow:yyyyMMdd HHmmss} {line}");
                    ProcessLineForStatusUpdate(line, jobModel.Keywords, jobModel.StdOutProcessing, ref intermediateExecutionStatus);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    outputWriter.WriteLine($"E {DateTimeOffset.UtcNow:yyyyMMdd HHmmss} {line}");
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
                    execution.StopReason = TerminationReason.Exited;
                }
                else
                {
                    execution.StopReason = TerminationReason.TimedOut;
                    execution.Status = ExecutionStatus.CompletedAsError;
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
                execution.StopReason = TerminationReason.Exited;
            }

            execution.CompletedOn = DateTimeOffset.UtcNow;
            execution.ExitCode = process.ExitCode;

            if (execution.Status == ExecutionStatus.Running)
            {
                if (execution.ExitCode != 0)
                {
                    // Always mark as error when the exit code is non-zero
                    execution.Status = ExecutionStatus.CompletedAsError;
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

    private static ExecutionIdentifier GetExecutionIdentifierFromModel(ExecutionModel execution)
    {
        return new ExecutionIdentifier(execution.JobId, execution.Id, execution.StartedOn);
    }
}

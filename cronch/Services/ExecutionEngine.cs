using cronch.Models;
using System.Diagnostics;

namespace cronch.Services;

public class ExecutionEngine(ILogger<ExecutionEngine> _logger)
{
    public virtual void PerformExecution(ExecutionModel execution, JobModel jobModel, string targetScriptFile, Stream outputStream, bool formatOutput, Dictionary<string, string> environmentVars, CancellationToken cancelToken)
    {
        var intermediateExecutionStatus = ExecutionStatus.CompletedAsSuccess;

        try
        {
            using var outputWriter = new StreamWriter(outputStream, leaveOpen: true);

            File.WriteAllText(targetScriptFile, jobModel.Script);
            if (!OperatingSystem.IsWindows())
            {
                try { File.SetUnixFileMode(targetScriptFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead); } catch { }
            }

            var executorFile = jobModel.Executor;
            var executorArgs = (jobModel.ExecutorArgs ?? "").Trim();
            if (executorArgs.Contains("{0}"))
            {
                executorArgs = string.Format(executorArgs, targetScriptFile);
            }
            else
            {
                executorArgs += $" {targetScriptFile}";
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
                Arguments = executorArgs,
            };
            environmentVars.ToList().ForEach(kvp => startInfo.EnvironmentVariables.Add(kvp.Key, kvp.Value));
            using var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lock (outputWriter)
                    {
                        outputWriter.WriteLine(formatOutput ? $"O {DateTimeOffset.UtcNow:yyyyMMdd HHmmss} {line}" : line);
                        outputWriter.Flush();
                        ProcessLineForStatusUpdate(line, jobModel.Keywords, jobModel.StdOutProcessing, ref intermediateExecutionStatus);
                    }
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lock (outputWriter)
                    {
                        outputWriter.WriteLine(formatOutput ? $"E {DateTimeOffset.UtcNow:yyyyMMdd HHmmss} {line}" : line);
                        outputWriter.Flush();
                        ProcessLineForStatusUpdate(line, jobModel.Keywords, jobModel.StdErrProcessing, ref intermediateExecutionStatus);
                    }
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var manuallyTerminated = false;
            using var _ = cancelToken.Register(() =>
            {
                manuallyTerminated = true;
                process.Kill(true);
            });

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

            if (manuallyTerminated)
            {
                execution.StopReason = TerminationReason.UserTriggered;
            }

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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while executing job '{Name}' ({Id}), execution {ExecName}", jobModel.Name, jobModel.Id, execution.GetExecutionName());

            try
            {
                execution.Status = ExecutionStatus.CompletedAsIndeterminate;
                execution.StopReason = TerminationReason.NoneSpecified;
            }
            catch (Exception)
            {
                // Ignore.
            }
        }
        finally
        {
            try
            {
                File.Delete(targetScriptFile);
            }
            catch (Exception)
            {
                // Ignore.
            }
        }
    }

    internal static void ProcessLineForStatusUpdate(string line, List<string> keywords, JobModel.OutputProcessing processingDirective, ref ExecutionStatus intermediateExecutionStatus)
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
}

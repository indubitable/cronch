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

    private readonly ILogger<JobExecutionService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<string, Thread> _executions = new();

    public JobExecutionService(ILogger<JobExecutionService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public virtual void ExecuteJob(JobModel jobModel, ExecutionReason reason)
    {
        var execution = ExecutionPersistenceModel.CreateNew(jobModel.Id, reason.ToString());
        var thread = new Thread(() => PerformExecution(execution, jobModel))
        {
            IsBackground = true
        };

        // Register the execution
        if (!_executions.TryAdd(execution.GetExecutionName(), thread))
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

        try
        {
            persistence.SaveExecution(execution);
            persistence.SaveLatestExecutionForJob(execution);

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
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                var line = args.Data;
                if (!string.IsNullOrWhiteSpace(line))
                {
                    stderrWriter.WriteLine($"{DateTimeOffset.UtcNow:yyyy-MM-dd_HH:mm:ss} {line}");
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            execution.CompletedOn = DateTimeOffset.UtcNow;
            execution.ExitCode = process.ExitCode;
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

            _executions.TryRemove(execution.GetExecutionName(), out var _);
        }
    }
}

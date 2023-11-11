using cronch.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace cronch.Services;

public class JobExecutionService
{
    public enum ExecutionReason
    {
        Scheduled = 0,
        ManualRun = 1,
    }

    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<string, Thread> _executions = new ConcurrentDictionary<string, Thread>();

    public JobExecutionService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void ExecuteJob(JobModel jobModel, ExecutionReason reason)
    {
        var execName = Utility.CreateExecutionName();
        var thread = new Thread(() => PerformExecution(execName, jobModel, reason))
        {
            IsBackground = true
        };

        // Register the execution
        if (!_executions.TryAdd(execName, thread))
        {
            throw new InvalidOperationException("Unable to execute job: could not register execution");
        }

        thread.Start();
    }

    private void PerformExecution(string executionId, JobModel jobModel, ExecutionReason reason)
    {
        var persistence = _serviceProvider.GetRequiredService<JobPersistenceService>();

        var tempDir = Path.GetTempPath();
        var scriptFile = Path.Combine(tempDir, Path.GetRandomFileName());
        if (!string.IsNullOrWhiteSpace(jobModel.ScriptFilePathname))
        {
            scriptFile = Path.Combine(tempDir, jobModel.ScriptFilePathname.Trim());
        }
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
            Arguments = executorArgs,
        };
        using var process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (sender, args) =>
        {
            var line = args.Data;
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            var line = args.Data;
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        // TODO: handle errors, deleting temp file, removing thread from concurrent dictionary, etc.
    }
}

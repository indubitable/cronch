using System.Diagnostics;

namespace cronch.Services;

/// <summary>
/// Monitors the parent process (if specified via CRONCH_ParentPID) and shuts down
/// if the parent process exits. This ensures child processes don't become orphans
/// when the parent test process is killed.
/// </summary>
public class ParentProcessMonitorService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ParentProcessMonitorService> _logger;
    private readonly int? _parentPid;

    public ParentProcessMonitorService(
        IHostApplicationLifetime lifetime,
        ILogger<ParentProcessMonitorService> logger)
    {
        _lifetime = lifetime;
        _logger = logger;

        var parentPidEnv = Environment.GetEnvironmentVariable("CRONCH_ParentPID");
        if (int.TryParse(parentPidEnv, out var pid))
        {
            _parentPid = pid;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_parentPid == null)
        {
            return;
        }

        _logger.LogInformation("Monitoring parent process {ParentPid}", _parentPid);

        try
        {
            var parentProcess = Process.GetProcessById(_parentPid.Value);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (parentProcess.HasExited)
                {
                    _logger.LogWarning("Parent process {ParentPid} has exited, shutting down", _parentPid);
                    _lifetime.StopApplication();
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (ArgumentException)
        {
            // Parent process doesn't exist (already exited)
            _logger.LogWarning("Parent process {ParentPid} not found, shutting down", _parentPid);
            _lifetime.StopApplication();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
    }
}

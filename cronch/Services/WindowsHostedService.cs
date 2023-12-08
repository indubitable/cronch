using System.Diagnostics;

namespace cronch.Services;

public class WindowsHostedService(ILogger<WindowsHostedService> _logger) : BackgroundService
{
    public static void InstallSelf()
    {
        if (ExecuteShellCommand("net.exe", "session") != 0)
        {
            Console.WriteLine("Please run this command from an elevated (Administrator) prompt.");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("Installing CRONCH! Windows Service...");
        using var currentProcess = Process.GetCurrentProcess();
        var binpath = currentProcess.MainModule!.FileName;
        var scReturnCode = ExecuteShellCommand("sc.exe", $"create \"CRONCH!\" binpath= \"{binpath}\" start= auto");
        if (scReturnCode != 0)
        {
            Console.WriteLine($"Unable to install CRONCH! Windows Service. Error code: {scReturnCode}");
            Environment.Exit(1);
            return;
        }
        ExecuteShellCommand("sc.exe", $"description \"CRONCH!\" \"Web-based job scheduler\"");
        Console.WriteLine("Installed! You can start the service like so:\r\nsc.exe start CRONCH!");
    }

    public static void UninstallSelf()
    {
        if (ExecuteShellCommand("net.exe", "session") != 0)
        {
            Console.WriteLine("Please run this command from an elevated (Administrator) prompt.");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("Uninstalling CRONCH! Windows Service...");
        ExecuteShellCommand("sc.exe", $"stop \"CRONCH!\"");
        var scReturnCode = ExecuteShellCommand("sc.exe", $"delete \"CRONCH!\"");
        if (scReturnCode != 0)
        {
            Console.WriteLine($"Unable to uninstall CRONCH! Windows Service. Error code: {scReturnCode}");
            Environment.Exit(1);
            return;
        }
        Console.WriteLine("Uninstalled!");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }

    private static int ExecuteShellCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(command, arguments)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    ErrorDialog = false,
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace cronch.SystemTests;

/// <summary>
/// Holds the app instance and temp data directory for a single test class.
/// Each test class gets its own isolated context for parallel execution.
/// </summary>
internal class TestClassContext : IDisposable
{
    private static readonly TimeSpan AppStartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AppShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly string _tempDirectory;
    private Process? _appProcess;

    public string BaseUrl { get; }

    public string TestRunId { get; }

    private TestClassContext(string tempDirectory, string baseUrl, Process appProcess)
    {
        _tempDirectory = tempDirectory;
        BaseUrl = baseUrl;
        TestRunId = Path.GetFileName(tempDirectory);
        _appProcess = appProcess;
    }

    public static async Task<TestClassContext> CreateAsync()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"cronch_systest_{Guid.NewGuid():N}");
        var configDir = Path.Combine(tempDirectory, "cronchconfig");
        var dataDir = Path.Combine(tempDirectory, "cronchdata");
        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(dataDir);

        var port = GetAvailablePort();
        var baseUrl = $"http://localhost:{port}";

        var appProcess = await StartAppAsync(configDir, dataDir, baseUrl);

        return new TestClassContext(tempDirectory, baseUrl, appProcess);
    }

    public void Dispose()
    {
        StopProcess(_appProcess);
        _appProcess = null;

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<Process> StartAppAsync(string configDir, string dataDir, string baseUrl)
    {
        var projectPath = FindProjectPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-build --no-launch-profile",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["CRONCH_ConfigLocation"] = configDir;
        startInfo.Environment["CRONCH_DataLocation"] = dataDir;

        // Pass parent PID so child process can exit if parent dies (handles hard kills)
        startInfo.Environment["CRONCH_ParentPID"] = Environment.ProcessId.ToString();

        var appProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the application process.");

        appProcess.BeginOutputReadLine();
        appProcess.BeginErrorReadLine();

        try
        {
            await WaitForAppReadyAsync(appProcess, baseUrl);
        }
        catch
        {
            StopProcess(appProcess);
            throw;
        }

        return appProcess;
    }

    private static string FindProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var projectPath = Path.Combine(directory.FullName, "cronch", "cronch.csproj");
            if (File.Exists(projectPath))
            {
                return projectPath;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find cronch.csproj. Make sure the solution structure is correct.");
    }

    private static async Task WaitForAppReadyAsync(Process appProcess, string baseUrl)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < AppStartupTimeout)
        {
            if (appProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Application process exited unexpectedly with code {appProcess.ExitCode}.");
            }

            try
            {
                var response = await client.GetAsync($"{baseUrl}/health");
                if (response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"Application did not become ready within {AppStartupTimeout.TotalSeconds} seconds.");
    }

    private static void StopProcess(Process? process)
    {
        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit((int)AppShutdownTimeout.TotalMilliseconds);
            }
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }
}

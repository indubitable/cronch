using System.Diagnostics;

namespace cronch.Services;

public class ProcessWrapper : IDisposable
{
    private Process _process = null!;

    public ProcessWrapper(ProcessStartInfo startInfo)
    {
        _process = new Process { StartInfo = startInfo };
    }

    protected ProcessWrapper() { }

    public virtual event DataReceivedEventHandler? OutputDataReceived
    {
        add => _process.OutputDataReceived += value;
        remove => _process.OutputDataReceived -= value;
    }

    public virtual event DataReceivedEventHandler? ErrorDataReceived
    {
        add => _process.ErrorDataReceived += value;
        remove => _process.ErrorDataReceived -= value;
    }

    public virtual void Start()
    {
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public virtual bool WaitForExit(TimeSpan timeout) => _process.WaitForExit(timeout);

    public virtual void WaitForExit() => _process.WaitForExit();

    public virtual void Kill() => _process.Kill(true);

    public virtual int GetExitCode() => _process.ExitCode;

    public void Dispose() => _process?.Dispose();
}

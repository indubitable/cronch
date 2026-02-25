using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text;

namespace cronch.Pages;

public class RuntimeStatsModel : PageModel
{
    public string Statistics { get; set; } = string.Empty;

    public void OnGet()
    {
        var stb = new StringBuilder();
        using var proc = Process.GetCurrentProcess();
        stb.AppendLine($"Run time                      : {DateTime.Now.Subtract(proc.StartTime)}");
        stb.AppendLine("----------");
        stb.AppendLine($"Physical memory size          : {HumanizeBytes(proc.WorkingSet64)}");
        stb.AppendLine($"Base priority                 : {proc.BasePriority}");
        stb.AppendLine($"Priority class                : {proc.PriorityClass}");
        stb.AppendLine($"User processor time           : {proc.UserProcessorTime}");
        stb.AppendLine($"Privileged processor time     : {proc.PrivilegedProcessorTime}");
        stb.AppendLine($"Total processor time          : {proc.TotalProcessorTime}");
        stb.AppendLine($"Paged system memory size      : {HumanizeBytes(proc.PagedSystemMemorySize64)}");
        stb.AppendLine($"Paged memory size             : {HumanizeBytes(proc.PagedMemorySize64)}");
        stb.AppendLine("----------");
        stb.AppendLine($"Peak paged memory size        : {HumanizeBytes(proc.PeakPagedMemorySize64)}");
        stb.AppendLine($"Peak physical memory size     : {HumanizeBytes(proc.PeakWorkingSet64)}");
        stb.AppendLine("----------");
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minAsyncThreads);
        stb.AppendLine($"Thread pool min threads       : {minWorkerThreads} worker, {minAsyncThreads} async I/O");
        ThreadPool.GetAvailableThreads(out var avaWorkerThreads, out var avaAsyncThreads);
        stb.AppendLine($"Thread pool available threads : {avaWorkerThreads} worker, {avaAsyncThreads} async I/O");
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxAsyncThreads);
        stb.AppendLine($"Thread pool max threads       : {maxWorkerThreads} worker, {maxAsyncThreads} async I/O");
        stb.AppendLine($"Thread pool current threads   : {ThreadPool.ThreadCount}");
        stb.AppendLine($"Thread pool queued work items : {ThreadPool.PendingWorkItemCount}");
        stb.AppendLine("----------");
        var allThreads = proc.Threads.OfType<ProcessThread>();
        var activeThreads = allThreads.Where(pt => pt.ThreadState == System.Diagnostics.ThreadState.Running);
        stb.AppendLine($"Process total threads         : {allThreads.Count()}");
        stb.AppendLine($"Process active threads        : {activeThreads.Count()}");

        Statistics = stb.ToString();
    }

    private static string HumanizeBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }
        if (bytes < 1048576)
        {
            return $"{(long)Math.Round(bytes / 1024.0)} KB";
        }
        if (bytes < 1073741824)
        {
            return $"{(long)Math.Round(bytes / 1048576.0)} MB";
        }
        return $"{(long)Math.Round(bytes / 1073741824.0)} GB";
    }
}

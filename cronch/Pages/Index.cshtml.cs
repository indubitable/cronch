using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class IndexModel(JobConfigService _jobConfigService, JobExecutionService _jobExecutionService) : PageModel
{
    public readonly record struct RunningExecution(string Name, TimeSpan Duration);

    public int EnabledJobCount { get; set; }
    public int TotalJobCount { get; set; }
    public List<RunningExecution> RunningJobs { get; set; } = [];
    public int LastWeekSuccesses { get; set; }
    public int LastWeekErrors { get; set; }
    public int LastWeekWarnings { get; set; }

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;

        RunningJobs = _jobExecutionService.GetCurrentExecutions()
            .OrderBy(e => e.StartedOn)
            .Select(e => new RunningExecution(allJobs.FirstOrDefault(j => j.Id == e.JobId)?.Name ?? string.Empty, DateTimeOffset.UtcNow.Subtract(e.StartedOn)))
            .Where(re => !string.IsNullOrWhiteSpace(re.Name))
            .Take(3)
            .ToList();

        var stats = _jobExecutionService.GetExecutionStatistics(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
        LastWeekSuccesses = stats.Successes;
        LastWeekErrors = stats.Errors;
        LastWeekWarnings = stats.Warnings;
    }
}
using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class IndexModel(JobConfigService _jobConfigService, JobExecutionService _jobExecutionService) : PageModel
{
    public int EnabledJobCount { get; set; }
    public int TotalJobCount { get; set; }
    public List<ExecutionViewModel> RunningJobs { get; set; } = [];
    public int LastWeekSuccesses { get; set; }
    public int LastWeekErrors { get; set; }
    public int LastWeekWarnings { get; set; }
    public List<ExecutionViewModel> RecentExecutions { get; set; } = [];

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;

        RunningJobs = _jobExecutionService.GetAllCurrentExecutions()
            .Select(e => new ExecutionViewModel(e.JobId, e.ExecutionId, allJobs.FirstOrDefault(j => j.Id == e.JobId)?.Name ?? string.Empty, e.StartedOn, null, Models.ExecutionStatus.Running, null, null))
            .Where(re => !string.IsNullOrWhiteSpace(re.JobName))
            .OrderBy(e => e.StartedOn)
            .ToList();

        var stats = _jobExecutionService.GetExecutionStatistics(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow);
        LastWeekSuccesses = stats.Successes;
        LastWeekErrors = stats.Errors;
        LastWeekWarnings = stats.Warnings;

        RecentExecutions = _jobExecutionService.GetRecentExecutions(15, null);
    }
}
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
    public List<ExecutionViewModel> RecentExecutions { get; set; } = [];

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;

        RunningJobs = _jobExecutionService.GetAllRunningExecutions()
            .Select(e => new ExecutionViewModel(e.JobId, e.ExecutionId, allJobs.FirstOrDefault(j => j.Id == e.JobId)?.Name ?? string.Empty, e.StartedOn, null, Models.ExecutionStatus.Running, null, null))
            .Where(re => !string.IsNullOrWhiteSpace(re.JobName))
            .OrderBy(e => e.StartedOn)
            .ToList();

        RecentExecutions = _jobExecutionService.GetRecentExecutions(15, null, null);
    }
}
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
    public int RunningJobCount { get; set; }
    public List<ExecutionViewModel> RecentExecutions { get; set; } = [];

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;

        RunningJobCount = _jobExecutionService.GetAllRunningExecutions().Count;

        RecentExecutions = _jobExecutionService.GetRecentExecutions(15, null, null);
    }
}
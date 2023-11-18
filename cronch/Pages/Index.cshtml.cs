using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class IndexModel(JobConfigService _jobConfigService) : PageModel
{
    public int EnabledJobCount { get; set; }
    public int TotalJobCount { get; set; }

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;
    }
}
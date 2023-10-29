using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly JobConfigService _jobConfigService;

    public int EnabledJobCount { get; set; }
    public int TotalJobCount { get; set; }

    public IndexModel(ILogger<IndexModel> logger, JobConfigService jobConfigService)
    {
        _logger = logger;
        _jobConfigService = jobConfigService;
    }

    public void OnGet()
    {
        var allJobs = _jobConfigService.GetAllJobs();
        EnabledJobCount = allJobs.Where(j => j.Enabled).Count();
        TotalJobCount = allJobs.Count;
    }
}
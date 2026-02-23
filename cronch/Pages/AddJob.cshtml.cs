using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class AddJobModel(JobConfigService _jobConfigService) : PageModel
{
    [BindProperty]
    public JobViewModel? JobVM { get; set; }

    public IActionResult OnGet()
    {
        ViewData["CurrentJobId"] = Guid.Empty;
        return Page();
    }

    public IActionResult OnGetCronPreview(string? cronSchedule)
    {
        if (string.IsNullOrWhiteSpace(cronSchedule))
            return Content(string.Empty, "text/html");
        try
        {
            return Content(CronDescriptionUtility.Describe(cronSchedule), "text/html");
        }
        catch
        {
            return Content("<span class=\"text-danger\">Invalid cron expression</span>", "text/html");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ViewData["CurrentJobId"] = Guid.Empty;
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        await _jobConfigService.CreateJobAsync(JobVM.ToModel(), true);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Manage");
            return StatusCode(204);
        }

        return RedirectToPage("/Manage");
    }
}

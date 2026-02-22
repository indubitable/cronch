using Cronos;
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
            CronExpression.Parse(cronSchedule, CronFormat.IncludeSeconds);
            return Content(CronExpressionDescriptor.ExpressionDescriptor.GetDescription(cronSchedule), "text/html");
        }
        catch
        {
            return Content("Invalid cron expression", "text/html");
        }
    }

    public IActionResult OnPost()
    {
        ViewData["CurrentJobId"] = Guid.Empty;
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        _jobConfigService.CreateJob(JobVM.ToModel(), true);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Manage");
            return StatusCode(204);
        }

        return RedirectToPage("/Manage");
    }
}

using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class EditJobModel(JobConfigService _jobConfigService) : PageModel
{
    [BindProperty]
    public JobViewModel JobVM { get; set; } = null!;

    public IActionResult OnGetCronPreview(Guid id, string? cronSchedule)
    {
        if (string.IsNullOrWhiteSpace(cronSchedule))
        {
            return Content(string.Empty, "text/html");
        }

        try
        {
            return Content(CronDescriptionUtility.Describe(cronSchedule), "text/html");
        }
        catch
        {
            return Content("<span class=\"text-danger\">Invalid cron expression</span>", "text/html");
        }
    }

    public IActionResult OnGet(Guid id)
    {
        var job = _jobConfigService.GetJob(id);
        if (job != null)
        {
            ViewData["CurrentJobId"] = id;
            JobVM = job.ToViewModel();
            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        ViewData["CurrentJobId"] = id;
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        var model = JobVM.ToModel();
        model.Id = id;
        await _jobConfigService.UpdateJobAsync(model);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Manage");
            return StatusCode(204);
        }

        return RedirectToPage("/Manage");
    }

}

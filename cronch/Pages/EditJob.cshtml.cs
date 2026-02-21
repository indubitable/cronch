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

    public IActionResult OnGet(Guid id)
    {
        var job = _jobConfigService.GetJob(id);
        if (job != null)
        {
            JobVM = job.ToViewModel();
            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public IActionResult OnPost(Guid id)
    {
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        var model = JobVM.ToModel();
        model.Id = id;
        _jobConfigService.UpdateJob(model);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Manage");
            return StatusCode(204);
        }

        return RedirectToPage("/Manage");
    }
}

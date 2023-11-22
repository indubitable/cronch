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
        return Page();
    }

    public IActionResult OnPost()
    {
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        _jobConfigService.CreateJob(JobVM.ToModel(), true);

        return RedirectToPage("/Manage");
    }
}

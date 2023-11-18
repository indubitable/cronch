using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class AddJobModel(JobConfigService _jobConfigService, ConfigConverterService _configConverterService) : PageModel
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

        _jobConfigService.CreateJob(_configConverterService.ConvertToModel(JobVM), true);

        return RedirectToPage("/Manage");
    }
}

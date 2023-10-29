using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cronch.Pages;

public class AddJobModel : PageModel
{
    private readonly JobConfigService _jobConfigService;
    private readonly ConfigConverterService _configConverterService;

    [BindProperty]
    public JobViewModel? JobVM { get; set; }

    public AddJobModel(JobConfigService jobConfigService, ConfigConverterService configConverterService)
    {
        _jobConfigService = jobConfigService;
        _configConverterService = configConverterService;
    }

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

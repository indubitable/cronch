using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cronch.Pages;

public class AddJobModel : PageModel
{
    private readonly JobConfigService _jobConfigService;

    [BindProperty]
    public JobViewModel? JobVM { get; set; }
    public List<SelectListItem> ProcessingOptions => JobViewModel.ProcessingOptions;

    public AddJobModel(JobConfigService jobConfigService)
    {
        _jobConfigService = jobConfigService;
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

        _jobConfigService.CreateJob(JobVM);

        return RedirectToPage("/Manage");
    }
}

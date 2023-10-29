using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class EditModel : PageModel
{
		private readonly JobConfigService _jobConfigService;
		private readonly ConfigConverterService _configConverterService;

    [BindProperty]
    public JobViewModel JobVM { get; set; } = null!;

    public EditModel(JobConfigService jobConfigService, ConfigConverterService configConverterService)
    {
        _jobConfigService = jobConfigService;
		_configConverterService = configConverterService;
	}

    public void OnGet(Guid id)
    {
        JobVM = _configConverterService.ConvertToViewModel(_jobConfigService.GetJob(id));
    }

    public IActionResult OnPost(Guid id)
    {
        if (JobVM == null || !ModelState.IsValid)
        {
            return Page();
        }

        var model = _configConverterService.ConvertToModel(JobVM);
        model.Id = id;
        _jobConfigService.UpdateJob(model);

        return RedirectToPage("/Manage");
    }
}

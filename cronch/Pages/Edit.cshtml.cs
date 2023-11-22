using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class EditModel(JobConfigService _jobConfigService) : PageModel
{
    [BindProperty]
    public JobViewModel JobVM { get; set; } = null!;

    public void OnGet(Guid id)
    {
        JobVM = _jobConfigService.GetJob(id).ToViewModel();
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

        return RedirectToPage("/Manage");
    }
}

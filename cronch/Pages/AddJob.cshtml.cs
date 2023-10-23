using cronch.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cronch.Pages;

public class AddJobModel : PageModel
{
    [BindProperty]
    public JobViewModel? JobVM { get; set; }

    public List<SelectListItem> ProcessingOptions => JobViewModel.ProcessingOptions;

    public IActionResult OnGet()
    {
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var tmp = JobVM;
        return RedirectToPage("./Index");
    }
}

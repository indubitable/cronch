using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class SettingsModel(SettingsService _settingsService) : PageModel
{
    [BindProperty]
    public SettingsViewModel SettingsVM { get; set; } = null!;

    public void OnGet()
    {
        SettingsVM = _settingsService.LoadSettings().ToViewModel();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _settingsService.SaveSettings(SettingsVM.ToModel());

        TempData["Message"] = "Settings have been saved.";
        TempData["MessageType"] = "success";

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Settings");
            return StatusCode(204);
        }

        return RedirectToPage("/Settings");
    }
}
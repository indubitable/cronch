using cronch.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class SettingsModel : PageModel
{
    [BindProperty]
    public SettingsViewModel SettingsVM { get; set; } = null!;

    public void OnGet()
    {
    }
}
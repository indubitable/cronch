using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class SettingsModel(SettingsService _settingsService, ConfigPersistenceService _configPersistenceService, JobConfigService _jobConfigService) : PageModel
{
    [BindProperty]
    public SettingsViewModel SettingsVM { get; set; } = null!;

    public void OnGet() => SettingsVM = _settingsService.LoadSettings().ToViewModel();

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

    public IActionResult OnGetExport()
    {
        var xmlBytes = _configPersistenceService.GetConfigXmlBytes();
        return File(xmlBytes, "application/xml", "cronch-config.xml");
    }

    public async Task<IActionResult> OnPostImportAsync(IFormFile? configFile)
    {
        if (configFile == null || configFile.Length == 0)
        {
            TempData["Message"] = "No file was selected.";
            TempData["MessageType"] = "danger";
            return RedirectToPage("/Settings");
        }

        try
        {
            using var stream = configFile.OpenReadStream();
            await _jobConfigService.ImportConfigAsync(stream);
        }
        catch (InvalidOperationException)
        {
            TempData["Message"] = "The uploaded file is not a valid cronch configuration.";
            TempData["MessageType"] = "danger";
            return RedirectToPage("/Settings");
        }

        TempData["Message"] = "Configuration has been imported successfully.";
        TempData["MessageType"] = "success";

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            Response.Headers.Append("HX-Redirect", "/Settings");
            return StatusCode(204);
        }

        return RedirectToPage("/Settings");
    }
}
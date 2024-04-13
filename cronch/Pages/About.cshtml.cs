using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Runtime.InteropServices;

namespace cronch.Pages;

public class AboutModel(IConfiguration _configuration) : PageModel
{
    public string ConfigLocation { get; set; } = string.Empty;
    public string ConfigLocationResolved { get; set; } = string.Empty;
    public string DataLocation { get; set; } = string.Empty;
    public string DataLocationResolved { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string AppFullVersion { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;

    public void OnGet()
    {
        ConfigLocation = _configuration["ConfigLocation"] ?? string.Empty;
        ConfigLocationResolved = Path.GetFullPath(ConfigLocation);
        DataLocation = _configuration["DataLocation"] ?? string.Empty;
        DataLocationResolved = Path.GetFullPath(DataLocation);

        var version = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
        AppFullVersion = version?.InformationalVersion ?? "Unknown";
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
        Runtime = RuntimeInformation.FrameworkDescription;
    }
}

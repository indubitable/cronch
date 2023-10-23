using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace cronch.Models.ViewModels;

public class JobViewModel
{
    public Guid Id { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Enabled")]
    public bool Enabled { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Cron Schedule")]
    public string CronSchedule { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Executor")]
    public string Executor { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Script")]
    public string Script { get; set; } = string.Empty;

    [Display(Name = "Script File", Description = "Optional pathname to use for script contents when they are written and executed")]
    public string? ScriptFilePathname { get; set; }

    [Display(Name = "Keywords", Description = "Comma-separated list of keywords to look for")]
    public string? Keywords { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Standard Out Processing")]
    public string StdOutProcessing { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Standard Error Processing")]
    public string StdErrProcessing { get; set; } = string.Empty;

    public static List<SelectListItem> ProcessingOptions
    {
        get
        {
            return new List<SelectListItem>
            {
                new SelectListItem("None", "None"),
				new SelectListItem("Mark as warning on any output", "WarningOnAnyOutput"),
				new SelectListItem("Mark as error on any output", "ErrorOnAnyOutput"),
				new SelectListItem("Mark as warning when a keyword matches", "WarningOnMatchingKeywords"),
				new SelectListItem("Mark as error when a keyword matches", "ErrorOnMatchingKeywords"),
			};
        }
    }
}

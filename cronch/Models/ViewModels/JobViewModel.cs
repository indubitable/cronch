using Cronos;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using static cronch.Models.JobModel;

namespace cronch.Models.ViewModels;

public class JobViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    // Editable fields:

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Enabled")]
    public bool Enabled { get; set; }

    [Display(Name = "Cron schedule")]
    public string? CronSchedule { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Executor")]
    public string Executor { get; set; } = string.Empty;

    [Display(Name = "Executor arguments")]
    public string? ExecutorArgs { get; set; }

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Script")]
    public string Script { get; set; } = string.Empty;

    [Display(Name = "Script file")]
    public string? ScriptFilePathname { get; set; }

    [Display(Name = "Execution time limit (in seconds)")]
    public double? TimeLimitSecs { get; set; }

    [Display(Name = "Parallelism")]
    [Range(1, 100)]
    public int? Parallelism { get; set; }

    [Required]
    [Display(Name = "Parallel execution skip handling")]
    public string MarkParallelSkipAs { get; set; } = string.Empty;

    [Display(Name = "Keywords")]
    public string? Keywords { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Standard Out processing")]
    public string StdOutProcessing { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Standard Error processing")]
    public string StdErrProcessing { get; set; } = string.Empty;

    public List<ChainRuleViewModel> ChainRules { get; set; } = [];

    // Reporting-only fields:

    public DateTimeOffset? LatestExecution { get; set; }

    public DateTimeOffset? NextExecution { get; set; }

    // Other:

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        ValidationResult? validationResult = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(CronSchedule))
            {
                CronExpression.Parse(CronSchedule, CronFormat.IncludeSeconds);
            }
        }
        catch (Exception ex)
        {
            validationResult = new ValidationResult($"The provided Cron schedule could not be parsed: {ex.Message}", new[] { nameof(CronSchedule) });
        }

        if (validationResult != null)
        {
            yield return validationResult;
        }
    }

    public string GetValidatedCronDescription()
    {
        try
        {
            CronExpression.Parse(CronSchedule, CronFormat.IncludeSeconds);
            return CronExpressionDescriptor.ExpressionDescriptor.GetDescription(CronSchedule);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static List<SelectListItem> OutputProcessingOptions
    {
        get
        {
            return new List<SelectListItem>
            {
                new SelectListItem("None", nameof(OutputProcessing.None)),
				new SelectListItem("Set job status to warning on any output", nameof(OutputProcessing.WarningOnAnyOutput)),
				new SelectListItem("Set job status to error on any output", nameof(OutputProcessing.ErrorOnAnyOutput)),
				new SelectListItem("Set job status to warning when a keyword matches", nameof(OutputProcessing.WarningOnMatchingKeywords)),
				new SelectListItem("Set job status to error when a keyword matches", nameof(OutputProcessing.ErrorOnMatchingKeywords)),
			};
        }
    }

    public static List<SelectListItem> ParallelSkipProcessingOptions
    {
        get
        {
            return new List<SelectListItem>
            {
                new SelectListItem("Ignore", nameof(ParallelSkipProcessing.Ignore)),
                new SelectListItem("Mark as indeterminate", nameof(ParallelSkipProcessing.MarkAsIndeterminate)),
                new SelectListItem("Mark as warning", nameof(ParallelSkipProcessing.MarkAsWarning)),
                new SelectListItem("Mark as error", nameof(ParallelSkipProcessing.MarkAsError)),
            };
        }
    }
}

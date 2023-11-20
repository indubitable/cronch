using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class ExecutionModel(JobExecutionService _jobExecutionService, JobConfigService _jobConfigService, ConfigConverterService _configConverterService) : PageModel
{
    public ExecutionViewModel Execution { get; set; }
    public JobViewModel? Job { get; set; }
    public string StdOut { get; set; } = string.Empty;
    public string StdErr { get; set; } = string.Empty;

    public void OnGet(Guid id)
    {
        Execution = _jobExecutionService.GetExecution(id);
        (StdOut, StdErr) = _jobExecutionService.GetStdOutAndStdErrForExecution(id);

        try
        {
            var jobModel = _jobConfigService.GetJob(Execution.JobId);
            Job = _configConverterService.ConvertToViewModel(jobModel);
        }
        catch (Exception)
        {
            // Job may have been deleted. Ignore.
        }
    }
}

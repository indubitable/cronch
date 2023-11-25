using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class ManageModel(JobConfigService _jobConfigService, JobExecutionService _jobExecutionService, JobSchedulingService _jobSchedulingService) : PageModel
{
    public List<JobViewModel> Jobs { get; set; } = [];

    public void OnGet()
    {
        Jobs = _jobConfigService.GetAllJobs()
            .Select(ConversionUtility.ToViewModel)
            .OrderByDescending(j => j.Enabled).ThenBy(j => j.Name)
            .ToList();

        PostProcessRetrievedJobs();
    }

    public IActionResult OnPostDeleteJob(Guid id)
    {
        _jobConfigService.DeleteJob(id);
        return RedirectToPage("/Manage");
    }

    public IActionResult OnPostRunJob(Guid id)
    {
        var job = _jobConfigService.GetJob(id);
        if (job != null)
        {
            _jobExecutionService.ExecuteJob(job, Models.ExecutionReason.Manual);
            TempData["Message"] = "Job started!";
            TempData["MessageType"] = "success";
        }
        else
        {
            TempData["Message"] = "Could not find job to start!";
            TempData["MessageType"] = "danger";
        }
        return RedirectToPage("/Manage");
    }

    public IActionResult OnPostMultiSelectAction([FromForm] string action, [FromForm] List<Guid> jobIds)
    {
        var verb = string.Empty;
        foreach (var jobId in jobIds)
        {
            var job = _jobConfigService.GetJob(jobId);

            if (action.Equals("enable"))
            {
                job.Enabled = true;
                verb = "enabled";
            }
            else if (action.Equals("disable"))
            {
                job.Enabled = false;
                verb = "disabled";
            }
            else
            {
                TempData["Message"] = "Unable to perform unknown action!";
                TempData["MessageType"] = "danger";
                return RedirectToPage("/Manage");
            }

            _jobConfigService.UpdateJob(job);
        }

        TempData["Message"] = $"Successfully {verb} {jobIds.Count} job(s)";
        TempData["MessageType"] = "success";
        return RedirectToPage("/Manage");
    }

    private void PostProcessRetrievedJobs()
    {
        var latestExecutionsPerJob = _jobExecutionService.GetLatestExecutions();
        foreach (var job in Jobs)
        {
            job.LatestExecution = (latestExecutionsPerJob.ContainsKey(job.Id) ? latestExecutionsPerJob[job.Id] : null);
            job.NextExecution = (job.Enabled ? _jobSchedulingService.GetNextExecution(job.Id) : null);
        }
    }
}

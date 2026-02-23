using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class ManageModel(JobConfigService _jobConfigService, JobExecutionService _jobExecutionService, JobSchedulingService _jobSchedulingService) : PageModel
{
    public List<JobViewModel> Jobs { get; set; } = [];

    public async Task OnGetAsync()
    {
        Jobs = _jobConfigService.GetAllJobs()
            .Select(ConversionUtility.ToViewModel)
            .OrderByDescending(j => j.Enabled).ThenBy(j => j.Name)
            .ToList();

        await PostProcessRetrievedJobsAsync();
    }

    public async Task<IActionResult> OnPostDeleteJobAsync(Guid id)
    {
        await _jobConfigService.DeleteJobAsync(id);
        return RedirectToPage("/Manage");
    }

    public async Task<IActionResult> OnPostDuplicateJobAsync(Guid originalJobId, string duplicateName)
    {
        var originalJob = _jobConfigService.GetJob(originalJobId);
        if (originalJob == null)
        {
            // This really shouldn't happen in normal circumstances...
            return RedirectToPage("/Manage");
        }

        var newJob = originalJob.ToPersistence().ToModel();
        newJob.Id = Guid.NewGuid();
        newJob.Name = duplicateName;
        newJob.Enabled = false;

        await _jobConfigService.CreateJobAsync(newJob, false);

        return RedirectToPage("/EditJob", new { id = newJob.Id });
    }

    public IActionResult OnPostRunJob(Guid id)
    {
        var job = _jobConfigService.GetJob(id);
        if (job != null)
        {
            var execId = _jobExecutionService.ExecuteJob(job, Models.ExecutionReason.Manual);
            TempData["Message"] = "Job started!";
            TempData["MessageType"] = "success";
            TempData["MessageLink"] = $"/ExecutionDetails/{execId:D}";
            TempData["MessageLinkName"] = "View execution details";
        }
        else
        {
            TempData["Message"] = "Could not find job to start!";
            TempData["MessageType"] = "danger";
        }
        return RedirectToPage("/Manage");
    }

    public async Task<IActionResult> OnPostMultiSelectActionAsync([FromForm] string action, [FromForm] List<Guid> jobIds)
    {
        var verb = string.Empty;
        foreach (var jobId in jobIds)
        {
            var job = _jobConfigService.GetJob(jobId);
            if (job != null)
            {
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

                await _jobConfigService.UpdateJobAsync(job);
            }
        }

        TempData["Message"] = $"Successfully {verb} {jobIds.Count} job(s)";
        TempData["MessageType"] = "success";
        return RedirectToPage("/Manage");
    }

    private async Task PostProcessRetrievedJobsAsync()
    {
        var latestExecutionsPerJob = _jobExecutionService.GetLatestExecutionsPerJob();
        foreach (var job in Jobs)
        {
            job.LatestExecution = (latestExecutionsPerJob.ContainsKey(job.Id) ? latestExecutionsPerJob[job.Id] : null);
            job.NextExecution = (job.Enabled ? await _jobSchedulingService.GetNextExecutionAsync(job.Id) : null);
        }
    }
}

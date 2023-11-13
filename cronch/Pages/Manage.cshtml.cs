using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages
{
    public class ManageModel : PageModel
    {
        private readonly JobConfigService _jobConfigService;
        private readonly ConfigConverterService _configConverterService;
        private readonly JobExecutionService _jobExecutionService;
        private readonly JobPersistenceService _jobPersistenceService;

        public List<JobViewModel> Jobs { get; set; } = new();

        public ManageModel(JobConfigService jobConfigService, ConfigConverterService configConverterService, JobExecutionService jobExecutionService, JobPersistenceService jobPersistenceService)
        {
            _jobConfigService = jobConfigService;
            _configConverterService = configConverterService;
            _jobExecutionService = jobExecutionService;
            _jobPersistenceService = jobPersistenceService;
        }

        public void OnGet()
        {
            Jobs = _jobConfigService.GetAllJobs().Select(_configConverterService.ConvertToViewModel).ToList();
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
                _jobExecutionService.ExecuteJob(job, JobExecutionService.ExecutionReason.Manual);
                //TempData["Message"] = "Job started!";
            }
            else
            {
                //TempData["Message"] = "Could not find job to start!";
            }
            return RedirectToPage("/Manage");
        }

        private void PostProcessRetrievedJobs()
        {
            foreach (var job in Jobs)
            {
                job.LatestExecution = _jobPersistenceService.GetLatestExecutionForJob(job.Id);
            }
        }
    }
}

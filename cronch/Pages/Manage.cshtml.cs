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

        public List<JobViewModel> Jobs { get; set; } = new();

        public ManageModel(JobConfigService jobConfigService, ConfigConverterService configConverterService)
        {
            _jobConfigService = jobConfigService;
            _configConverterService = configConverterService;
        }

        public void OnGet()
        {
            Jobs = _jobConfigService.GetAllJobs().Select(_configConverterService.ConvertToViewModel).ToList();
        }

        public IActionResult OnPostDeleteJob(Guid id)
        {
            _jobConfigService.DeleteJob(id);
            return RedirectToPage("/Manage");
        }
    }
}

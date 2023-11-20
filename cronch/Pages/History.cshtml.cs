using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class HistoryModel(JobExecutionService _jobExecutionService) : PageModel
{
    public int MaxResults { get; init; } = 250;
    public List<ExecutionViewModel> Executions { get; set; } = [];

    public void OnGet()
    {
        Executions = _jobExecutionService.GetRecentExecutions(MaxResults);
    }
}

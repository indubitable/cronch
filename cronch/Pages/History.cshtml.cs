using cronch.Models.ViewModels;
using cronch.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cronch.Pages;

public class HistoryModel(JobExecutionService _jobExecutionService, SettingsService _settingsService) : PageModel
{
    public int MaxResults { get; set; }
    public List<ExecutionViewModel> Executions { get; set; } = [];

    public void OnGet()
    {
        MaxResults = _settingsService.LoadSettings().MaxHistoryItemsShown ?? SettingsService.DefaultMaxHistoryItemsShown;
        Executions = _jobExecutionService.GetRecentExecutions(MaxResults);
    }
}

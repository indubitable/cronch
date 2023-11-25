using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Encodings.Web;

namespace cronch.Pages;

public class ExecutionDetailsModel(JobExecutionService _jobExecutionService, JobConfigService _jobConfigService, HtmlEncoder _htmlEncoder) : PageModel
{
    public ExecutionViewModel Execution { get; set; }
    public JobViewModel? Job { get; set; }
    public string JobOutputProcessed { get; set; } = string.Empty;

    public void OnGet(Guid id)
    {
        Execution = _jobExecutionService.GetExecution(id);
        JobOutputProcessed = ProcessJobOutput(_jobExecutionService.GetOutputForExecution(id));

        try
        {
            Job = _jobConfigService.GetJob(Execution.JobId).ToViewModel();
        }
        catch (Exception)
        {
            // Job may have been deleted. Ignore.
        }
    }

    private string ProcessJobOutput(string output)
    {
        var stb = new StringBuilder();
        foreach(var line in output.Split('\n'))
        {
            var l = line.Trim();
            if (l.Length >= 18 && l[1] == ' ' && l[10] == ' ' && l[17] == ' ')
            {
                // Process this line
                var spanClass = (l[0] == 'E' ? "stderr" : "stdout");
                if (DateTimeOffset.TryParseExact(l.AsSpan(2, 15), "yyyyMMdd HHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var timestamp))
                {
                    stb.AppendLine($"<span class=\"{spanClass}\"><span class=\"unselectable me-2\">{timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}</span>{l.AsSpan(18..)}</span>");
                }
            }
            else
            {
                // Append this line as is, because something is odd about it
                stb.AppendLine($"<span>{_htmlEncoder.Encode(l)}</span>");
            }
        }
        return stb.ToString();
    }
}

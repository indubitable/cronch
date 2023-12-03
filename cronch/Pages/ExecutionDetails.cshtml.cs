using cronch.Models.ViewModels;
using cronch.Services;
using cronch.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Encodings.Web;

namespace cronch.Pages;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ExecutionDetailsModel(JobExecutionService _jobExecutionService, JobConfigService _jobConfigService, HtmlEncoder _htmlEncoder) : PageModel
{
    public ExecutionViewModel Execution { get; set; }
    public JobViewModel? Job { get; set; }
    public string JobOutputProcessed { get; set; } = string.Empty;
    public int JobOutputProcessedLines { get; set; }

    public void OnGet(Guid id, [FromQuery] int? lastLineCount)
    {
        Execution = _jobExecutionService.GetExecution(id);
        JobOutputProcessed = ProcessJobOutput(_jobExecutionService.GetOutputForExecution(id), lastLineCount ?? 0);

        try
        {
            Job = _jobConfigService.GetJob(Execution.JobId).ToViewModel();
        }
        catch (Exception)
        {
            // Job may have been deleted. Ignore.
        }
    }

    private string ProcessJobOutput(string output, int lastLineCount)
    {
        var stb = new StringBuilder();
        var splitLines = output.TrimEnd('\r', '\n').Split('\n');
        JobOutputProcessedLines = splitLines.Length; // at least for now, the number of raw lines is the same as the number of processed lines
        foreach(var line in splitLines.Skip(lastLineCount))
        {
            var l = line.Trim();
            if (l.Length >= 18 && l[1] == ' ' && l[10] == ' ' && l[17] == ' ')
            {
                // Process this line
                var spanClass = (l[0] == 'E' ? "stderr" : "stdout");
                if (DateTimeOffset.TryParseExact(l.AsSpan(2, 15), "yyyyMMdd HHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var timestamp))
                {
                    stb.Append($"<span class=\"{spanClass}\"><span class=\"unselectable timestamp me-2\">{timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}</span>{l.AsSpan(18..)}\r\n</span>");
                }
            }
            else
            {
                // Append this line as is, because something is odd about it
                stb.Append($"<span>{_htmlEncoder.Encode(l)}\r\n</span>");
            }
        }
        return stb.ToString();
    }
}

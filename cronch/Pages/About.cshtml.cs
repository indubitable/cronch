using cronch.Models.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Xml.Serialization;

namespace cronch.Pages;

public class AboutModel : PageModel
{
    public IConfiguration Configuration { get; set; }
    public string? OutputData { get; set; }

    public AboutModel(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void OnGet()
    {
        var tempJob = new JobPersistenceModel
        {
            Id = Guid.NewGuid(),
            Enabled = true,
            Name = "Home-A",
            CronSchedule = "* * * * *",
            Executor = "/bin/bash",
            Script = "echo ok",
        };
        var tempJob2 = new JobPersistenceModel
        {
            Id = Guid.NewGuid(),
            Enabled = true,
            Name = "Home-B",
            CronSchedule = "* * * * *",
            Executor = "/bin/bash",
            Script = "echo ok",
        };
        var jobs = new List<JobPersistenceModel>(new[] { tempJob, tempJob2 });
        jobs.Sort((a, b) => a.Id.CompareTo(b.Id));
        var tempJobs = new ConfigPersistenceModel { Jobs = jobs };
        var serializer = new XmlSerializer(typeof(ConfigPersistenceModel));
        using var writer = new Utf8StringWriter();
        serializer.Serialize(writer, tempJobs);
        OutputData = writer.ToString();
    }
}

public class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}

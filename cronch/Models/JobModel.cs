namespace cronch.Models;

[Serializable]
public class JobModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool Enabled { get; set; }
    public string CronSchedule { get; set; } = null!;
    public string Executor { get; set; } = null!;
    public string Script { get; set; } = null!;
    public string ScriptFilePathname { get; set; } = null!;
}

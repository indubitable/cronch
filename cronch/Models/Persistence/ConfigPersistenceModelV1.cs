using System.Xml.Serialization;

namespace cronch.Models.Persistence;

/// <summary>
/// Legacy v1 configuration model, used only for reading old config files during migration.
/// The v1 format uses Cronos-style cron expressions.
/// </summary>
[Serializable, XmlRoot("Configuration", Namespace = "urn:indubitable-software:cronch:v1", IsNullable = false)]
public class ConfigPersistenceModelV1
{
    [XmlArray(Order = 0), XmlArrayItem("Job")]
    public List<JobPersistenceModel> Jobs { get; set; } = [];

    [XmlElement(Order = 1, IsNullable = true)]
    public int? MaxHistoryItemsShown { get; set; }

    [XmlElement(Order = 2, IsNullable = true)]
    public int? DeleteHistoricalRunsAfterCount { get; set; }

    [XmlElement(Order = 3, IsNullable = true)]
    public int? DeleteHistoricalRunsAfterDays { get; set; }

    [XmlElement(Order = 4, IsNullable = true)]
    public string? DefaultScriptFileLocation { get; set; }

    [XmlElement(Order = 5, IsNullable = true)]
    public string? CompletionScriptExecutor { get; set; }

    [XmlElement(Order = 6, IsNullable = true)]
    public string? CompletionScriptExecutorArgs { get; set; }

    [XmlElement(Order = 7, IsNullable = true)]
    public string? CompletionScript { get; set; }

    [XmlArray(Order = 8), XmlArrayItem("Status")]
    public List<string> RunCompletionScriptOn { get; set; } = [];

    [XmlElement(Order = 9, IsNullable = true)]
    public int? MaxChainDepth { get; set; }

    /// <summary>
    /// Converts this v1 model to a v2 <see cref="ConfigPersistenceModel"/>,
    /// migrating cron expressions from Cronos format to Quartz.NET format.
    /// </summary>
    public ConfigPersistenceModel ToV2()
    {
        var v2 = new ConfigPersistenceModel
        {
            Jobs = Jobs.Select(j =>
            {
                var converted = Utilities.CronExpressionConverter.TryConvertCronosToQuartz(j.CronSchedule);
                return new JobPersistenceModel
                {
                    Id = j.Id,
                    Name = j.Name,
                    Enabled = converted != null ? j.Enabled : false, // Disable job if cron conversion fails
                    CronSchedule = converted ?? j.CronSchedule, // Keep original if conversion fails
                    Executor = j.Executor,
                    ExecutorArgs = j.ExecutorArgs,
                    Script = j.Script,
                    ScriptFilePathname = j.ScriptFilePathname,
                    TimeLimitSecs = j.TimeLimitSecs,
                    Parallelism = j.Parallelism,
                    MarkParallelSkipAs = j.MarkParallelSkipAs,
                    Keywords = new List<string>(j.Keywords),
                    StdOutProcessing = j.StdOutProcessing,
                    StdErrProcessing = j.StdErrProcessing,
                    ChainRules = new List<ChainRulePersistenceModel>(j.ChainRules),
                };
            }).ToList(),
            MaxHistoryItemsShown = MaxHistoryItemsShown,
            DeleteHistoricalRunsAfterCount = DeleteHistoricalRunsAfterCount,
            DeleteHistoricalRunsAfterDays = DeleteHistoricalRunsAfterDays,
            DefaultScriptFileLocation = DefaultScriptFileLocation,
            CompletionScriptExecutor = CompletionScriptExecutor,
            CompletionScriptExecutorArgs = CompletionScriptExecutorArgs,
            CompletionScript = CompletionScript,
            RunCompletionScriptOn = new List<string>(RunCompletionScriptOn),
            MaxChainDepth = MaxChainDepth,
        };

        return v2;
    }
}

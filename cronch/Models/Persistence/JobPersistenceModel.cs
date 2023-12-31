﻿using System.Xml.Serialization;

namespace cronch.Models.Persistence;

public class JobPersistenceModel
{
    [XmlElement(Order = 0)]
    public Guid Id { get; set; }

    [XmlElement(Order = 1)]
    public string Name { get; set; } = string.Empty;

    [XmlElement(Order = 2)]
    public bool Enabled { get; set; }

    [XmlElement(Order = 3)]
    public string CronSchedule { get; set; } = string.Empty;

    [XmlElement(Order = 4)]
    public string Executor { get; set; } = string.Empty;


    [XmlElement(Order = 5, IsNullable = true)]
    public string? ExecutorArgs { get; set; }

    [XmlElement(Order = 6)]
    public string Script { get; set; } = string.Empty;

    [XmlElement(Order = 7, IsNullable = true)]
    public string? ScriptFilePathname { get; set; }

    [XmlElement(Order = 8, IsNullable = true)]
    public double? TimeLimitSecs { get; set; }

    [XmlElement(Order = 9, IsNullable = true)]
    public int? Parallelism { get; set; }

    [XmlElement(Order = 10)]
    public string MarkParallelSkipAs { get; set; } = string.Empty;

    [XmlArray(Order = 11), XmlArrayItem("Keyword")]
    public List<string> Keywords { get; set; } = [];

    [XmlElement(Order = 12)]
    public string StdOutProcessing { get; set; } = string.Empty;

    [XmlElement(Order = 13)]
    public string StdErrProcessing { get; set; } = string.Empty;
}

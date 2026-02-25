using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace cronch.SystemTests;

/// <summary>
/// Condition attribute that skips system tests unless explicitly enabled.
/// Tests run when:
/// - Debugger is attached (e.g., debugging from Test Explorer)
/// - RUN_SYSTEM_TESTS environment variable is set to "true"
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SystemTestConditionAttribute : ConditionBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemTestConditionAttribute"/> class.
    /// </summary>
    public SystemTestConditionAttribute()
        : base(ConditionMode.Include)
    {
        IgnoreMessage = "System tests are skipped by default. Debug the test or set RUN_SYSTEM_TESTS=true to run.";
    }

    /// <inheritdoc />
    public override string GroupName => nameof(SystemTestConditionAttribute);

    /// <inheritdoc />
    public override bool IsConditionMet =>
        Debugger.IsAttached ||
        Environment.GetEnvironmentVariable("RUN_SYSTEM_TESTS") == "true";
}

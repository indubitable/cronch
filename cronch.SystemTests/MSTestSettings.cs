using System.Diagnostics.CodeAnalysis;

[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
[assembly: ExcludeFromCodeCoverage]

namespace cronch.SystemTests;

[TestClass]
public static class MSTestSettings
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext _)
    {
        // Register cleanup handlers for when the test process is killed gracefully
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SystemTestBase.Cleanup();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            SystemTestBase.Cleanup();
            Environment.Exit(0);
        };
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        SystemTestBase.Cleanup();
    }
}

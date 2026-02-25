using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Playwright;

namespace cronch.SystemTests;

/// <summary>
/// Base class for system tests using Playwright.
/// Each test class gets its own app instance and temp data directory for parallel execution.
/// When debugging, runs with a visible browser and slower actions for easier inspection.
/// </summary>
public abstract class SystemTestBase
{
    private static readonly ConcurrentDictionary<Type, TestClassContext> _contexts = new();

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private TestClassContext? _context;

    protected static bool IsDebugging => Debugger.IsAttached;

    protected IPage Page { get; private set; } = null!;

    protected string BaseUrl => _context?.BaseUrl ?? throw new InvalidOperationException("Test not initialized.");

    protected string TestRunId => _context?.TestRunId ?? throw new InvalidOperationException("Test not initialized.");

    internal static void Cleanup()
    {
        foreach (var context in _contexts.Values)
        {
            context.Dispose();
        }

        _contexts.Clear();
    }

    [TestInitialize]
    public virtual async Task TestInitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !IsDebugging,
            SlowMo = IsDebugging ? 250 : null,
        });
        _context = await GetOrCreateContextAsync();
        Page = await _browser.NewPageAsync(new BrowserNewPageOptions());

        // Use longer timeout when debugging to allow time for inspection
        Page.SetDefaultTimeout(IsDebugging ? 30_000 : 10_000);
    }

    [TestCleanup]
    public virtual async Task TestCleanupAsync()
    {
        await Page.CloseAsync();
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    /// <summary>
    /// Creates a job via the Add Job UI form and returns to the Manage page.
    /// Uses pwsh (PowerShell Core) as the executor for cross-platform compatibility.
    /// </summary>
    protected async Task CreateJobViaUiAsync(
        string name,
        string script,
        string? cronSchedule = null,
        bool enabled = true,
        double? timeLimitSecs = null,
        int? parallelism = null,
        string? markParallelSkipAs = null,
        string? stdOutProcessing = null,
        string? stdErrProcessing = null,
        string? keywords = null)
    {
        await Page.GotoAsync($"{BaseUrl}/AddJob");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The partial renders field names without the "JobVM." prefix
        await Page.FillAsync("input[name='Name']", name);

        var enabledCheckbox = Page.Locator("#Enabled");
        if (enabled != await enabledCheckbox.IsCheckedAsync())
        {
            await enabledCheckbox.SetCheckedAsync(enabled);
        }

        if (!string.IsNullOrEmpty(cronSchedule))
        {
            await Page.FillAsync("input[name='CronSchedule']", cronSchedule);
        }

        // Set the hidden script field directly (Ace editor syncs from it)
        await Page.EvaluateAsync($"document.getElementById('script-hidden').value = {System.Text.Json.JsonSerializer.Serialize(script)}");

        // Use pwsh (PowerShell Core) as executor for cross-platform compatibility
        await Page.FillAsync("input[name='ScriptFilePathname']", "{0}.ps1");

        await Page.FillAsync("input[name='Executor']", "pwsh");
        await Page.FillAsync("input[name='ExecutorArgs']", "-NoProfile -NonInteractive -File");

        if (timeLimitSecs.HasValue)
        {
            await Page.FillAsync("input[name='TimeLimitSecs']", timeLimitSecs.Value.ToString());
        }

        if (parallelism.HasValue)
        {
            await Page.FillAsync("input[name='Parallelism']", parallelism.Value.ToString());
        }

        if (!string.IsNullOrEmpty(markParallelSkipAs))
        {
            await Page.SelectOptionAsync("select[name='MarkParallelSkipAs']", markParallelSkipAs);
        }

        if (!string.IsNullOrEmpty(stdOutProcessing))
        {
            await Page.SelectOptionAsync("select[name='StdOutProcessing']", stdOutProcessing);
        }

        if (!string.IsNullOrEmpty(stdErrProcessing))
        {
            await Page.SelectOptionAsync("select[name='StdErrProcessing']", stdErrProcessing);
        }

        if (!string.IsNullOrEmpty(keywords))
        {
            await Page.FillAsync("input[name='Keywords']", keywords);
        }

        // Submit the form via direct POST to avoid htmx/Ace timing issues
        await Page.EvaluateAsync(@"
            const form = document.querySelector('#job-form-container form');
            form.removeAttribute('hx-boost');
            form.submit();
        ");

        await Page.WaitForURLAsync($"{BaseUrl}/Manage");
    }

    /// <summary>
    /// Gets the internal job ID for a named job by inspecting the Manage page HTML.
    /// </summary>
    protected async Task<string> GetJobIdFromManagePageAsync(string jobName)
    {
        await Page.GotoAsync($"{BaseUrl}/Manage");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The Run form's hidden input has the job ID
        var row = Page.Locator("tr", new() { Has = Page.Locator($"td:text-is('{jobName}')") });
        var hiddenInput = row.Locator("form input[name='id']").First;
        var id = await hiddenInput.GetAttributeAsync("value");
        Assert.IsNotNull(id, $"Could not find job ID for '{jobName}'");
        return id;
    }

    /// <summary>
    /// Adds a chain rule to a job via the Edit Job page using direct form manipulation.
    /// </summary>
    protected async Task AddChainRuleToJobAsync(string jobName, string targetJobId, bool onSuccess = false, bool onError = false)
    {
        await Page.GotoAsync($"{BaseUrl}/Manage");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = Page.Locator("tr", new() { Has = Page.Locator($"td:text-is('{jobName}')") });
        await row.Locator("a:text('Configure')").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the "Add chain rule" button to add a new chain rule row
        await Page.ClickAsync("#chain-rule-add-btn");

        // Select the target job in the new chain rule's dropdown
        var chainRow = Page.Locator(".chain-rule-row").Last;
        await chainRow.Locator("select").SelectOptionAsync(targetJobId);

        // Check the appropriate trigger checkboxes
        if (onSuccess)
        {
            await chainRow.Locator("input[id$='-success']").CheckAsync();
        }
        if (onError)
        {
            await chainRow.Locator("input[id$='-error']").CheckAsync();
        }

        // Submit the form
        await Page.EvaluateAsync(@"
            const form = document.querySelector('#job-form-container form');
            form.removeAttribute('hx-boost');
            form.submit();
        ");

        await Page.WaitForURLAsync($"{BaseUrl}/Manage");
    }

    /// <summary>
    /// Runs a job and returns the execution details URL from the toast link.
    /// </summary>
    protected async Task<string> RunJobAndGetExecutionUrlAsync(string jobName)
    {
        await RunJobFromManagePageAsync(jobName);

        var detailsLink = Page.Locator(".toast-body a:text('View execution details')");
        var href = await detailsLink.GetAttributeAsync("href");
        Assert.IsNotNull(href, "Expected toast with execution details link");
        return $"{BaseUrl}{href}";
    }

    /// <summary>
    /// Triggers a manual run for a job from the Manage page and returns the execution details URL.
    /// Assumes the Manage page is loaded or navigates to it.
    /// </summary>
    protected async Task<string> RunJobFromManagePageAsync(string jobName)
    {
        await Page.GotoAsync($"{BaseUrl}/Manage");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the row containing the job name and click its Run button
        var row = Page.Locator("tr", new() { Has = Page.Locator($"td:text-is('{jobName}')") });
        await row.Locator("button:text('Run')").ClickAsync();

        // After redirect, the toast contains a link to execution details
        await Page.WaitForURLAsync($"{BaseUrl}/Manage");
        return Page.Url;
    }

    /// <summary>
    /// Waits for an execution to complete by polling the execution details page.
    /// </summary>
    protected async Task<string> WaitForExecutionCompleteAsync(string executionDetailsUrl, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + timeout.Value;

        await Page.GotoAsync(executionDetailsUrl);

        while (DateTime.UtcNow < deadline)
        {
            var statusCell = Page.Locator("#job-metadata table td:right-of(th:text('Status'))").First;
            var statusText = (await statusCell.TextContentAsync())?.Trim() ?? "";

            if (!statusText.Equals("Running", StringComparison.OrdinalIgnoreCase))
            {
                return statusText;
            }

            await Task.Delay(500);
            await Page.ReloadAsync();
        }

        throw new TimeoutException($"Execution did not complete within {timeout.Value.TotalSeconds} seconds.");
    }

    private async Task<TestClassContext> GetOrCreateContextAsync()
    {
        var testClassType = GetType();

        if (_contexts.TryGetValue(testClassType, out var existing))
        {
            return existing;
        }

        var context = await TestClassContext.CreateAsync();

        if (!_contexts.TryAdd(testClassType, context))
        {
            // Another thread created it first, dispose ours and use theirs
            context.Dispose();
            return _contexts[testClassType];
        }

        return context;
    }
}

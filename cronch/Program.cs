using cronch.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CRONCH_");

#if DEBUG
builder.Logging.AddFilter("Microsoft.AspNetCore.Watch", LogLevel.Warning);
#endif

if (OperatingSystem.IsWindows())
{
    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

    if (WindowsServiceHelpers.IsWindowsService())
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;
    }
    else if (args.Length == 1 && args[0].Equals("-i"))
    {
        WindowsHostedService.InstallSelf();
        return;
    }
    else if (args.Length == 1 && args[0].Equals("-u"))
    {
        WindowsHostedService.UninstallSelf();
        return;
    }
}

// Add services to the container.
var mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	mvcBuilder.AddRazorRuntimeCompilation();
}

if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options => options.ServiceName = "CRONCH!");
    builder.Services.AddHostedService<WindowsHostedService>();
}

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddSingleton<JobConfigService>();
builder.Services.AddSingleton<ConfigPersistenceService>();
builder.Services.AddSingleton<JobExecutionService>();
builder.Services.AddSingleton<JobSchedulingService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<CleanupService>();
builder.Services.AddSingleton<FileAccessWrapper>();
builder.Services.AddSingleton<ProcessFactory>();
builder.Services.AddSingleton<ExecutionPersistenceService>();

builder.Services.AddTransient<ExecutionEngine>();

builder.Services.AddHostedService<ParentProcessMonitorService>();

builder.Services.AddHealthChecks()
    .AddCheck<CronchHealthCheck>("cronch");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}
app.UseStatusCodePagesWithReExecute("/Error");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var execPersistenceService = services.GetRequiredService<ExecutionPersistenceService>();
    execPersistenceService.InitializeDatabase();

    var cleanupService = services.GetRequiredService<CleanupService>();
    cleanupService.Initialize();

    var configService = services.GetRequiredService<JobConfigService>();
    var schedulingService = services.GetRequiredService<JobSchedulingService>();
    await schedulingService.RefreshSchedulesAsync(configService.GetAllJobs());
}

app.MapStaticAssets();

app.UseRouting();

app.UseResponseCaching();

app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();

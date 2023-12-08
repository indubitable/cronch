using cronch;
using cronch.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CRONCH_");

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
builder.Services.AddRazorPages();

var dataLocation = builder.Configuration["DataLocation"] ?? throw new ArgumentNullException("DataLocation", "The DataLocation configuration option is missing");
Directory.CreateDirectory(dataLocation);
var dbFile = Path.GetFullPath(Path.Combine(dataLocation, "executions.db"));
builder.Services.AddDbContext<CronchDbContext>(options => options.UseSqlite($"Data Source={dbFile}"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "CRONCH!";
    });
    builder.Services.AddHostedService<WindowsHostedService>();
}

builder.Services.AddSingleton<JobConfigService>();
builder.Services.AddSingleton<ConfigPersistenceService>();
builder.Services.AddSingleton<JobExecutionService>();
builder.Services.AddSingleton<JobSchedulingService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<CleanupService>();

builder.Services.AddTransient<ExecutionPersistenceService>();
builder.Services.AddTransient<ExecutionEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<CronchDbContext>();
    dbContext.Database.Migrate();
    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    var cleanupService = services.GetRequiredService<CleanupService>();
    cleanupService.Initialize();

    var schedulingService = services.GetRequiredService<JobSchedulingService>();
    schedulingService.StartSchedulingRuns();

    var configService = services.GetRequiredService<JobConfigService>();
    schedulingService.RefreshSchedules(configService.GetAllJobs());
}

app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

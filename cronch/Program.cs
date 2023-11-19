using cronch;
using cronch.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CRONCH_");

// Add services to the container.
builder.Services.AddRazorPages();

var dataLocation = builder.Configuration["DataLocation"] ?? throw new ArgumentNullException("DataLocation", "The DataLocation configuration option is missing");
var dbFile = Path.GetFullPath(Path.Combine(dataLocation, "executions.db"));
builder.Services.AddDbContext<CronchDbContext>(options => options.UseSqlite($"Data Source={dbFile}"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<ConfigConverterService>();
builder.Services.AddSingleton<JobConfigService>();
builder.Services.AddSingleton<ConfigPersistenceService>();
builder.Services.AddSingleton<JobExecutionService>();
builder.Services.AddSingleton<JobSchedulingService>();

builder.Services.AddTransient<ExecutionPersistenceService>();

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
    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");

    var schedulingService = services.GetRequiredService<JobSchedulingService>();
    schedulingService.StartSchedulingRuns();

    var configService = services.GetRequiredService<JobConfigService>();
    schedulingService.RefreshSchedules(configService.GetAllJobs());
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

using cronch.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ConfigConverterService>();
builder.Services.AddSingleton<JobConfigService>();
builder.Services.AddSingleton<ConfigPersistenceService>();
builder.Services.AddSingleton<JobExecutionService>();
builder.Services.AddSingleton<JobSchedulingService>();

builder.Services.AddTransient<JobPersistenceService>();

builder.Configuration.AddEnvironmentVariables(prefix: "CRONCH_");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

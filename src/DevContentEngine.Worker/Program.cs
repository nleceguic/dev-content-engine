using DevContentEngine.Application;
using DevContentEngine.Infrastructure;
using DevContentEngine.Worker.Hangfire;
using DevContentEngine.Worker.Jobs;
using DevContentEngine.Worker.Scheduling;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DevContentEngine.Worker")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "worker-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<DailyContentSchedule>();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:Default' is not configured. Set the ConnectionStrings__Default environment variable.");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString))
    .UseFilter(new JobLoggingFilter()));

builder.Services.AddHangfireServer();

var dashboardUsername = builder.Configuration["Hangfire:DashboardUsername"];
var dashboardPassword = builder.Configuration["Hangfire:DashboardPassword"];

if (string.IsNullOrWhiteSpace(dashboardUsername) || string.IsNullOrWhiteSpace(dashboardPassword))
{
    throw new InvalidOperationException(
        "Hangfire dashboard credentials are not configured. Set the Hangfire__DashboardUsername and " +
        "Hangfire__DashboardPassword environment variables.");
}

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireBasicAuthFilter(dashboardUsername, dashboardPassword)],
    DisplayStorageConnectionString = false
});

var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
var madridTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

recurringJobManager.AddOrUpdate<GenerateDailyContentJob>(
    "generate-daily-content",
    job => job.RunAsync(CancellationToken.None),
    DailyContentSchedule.CronExpressionText,
    new RecurringJobOptions { TimeZone = madridTimeZone });

var schedule = app.Services.GetRequiredService<DailyContentSchedule>();
Log.Information("Next daily content generation run scheduled for {NextRunUtc:u} UTC.", schedule.GetNextOccurrenceUtc());

if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/trigger-daily-content", (IBackgroundJobClient backgroundJobClient) =>
    {
        var jobId = backgroundJobClient.Enqueue<GenerateDailyContentJob>(job => job.RunAsync(CancellationToken.None));

        return Results.Accepted(value: new { jobId });
    });
}

app.Run();

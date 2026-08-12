using System.Diagnostics;
using Hangfire.Common;
using Hangfire.Server;
using Serilog;
using Serilog.Context;

namespace DevContentEngine.Worker.Hangfire;

public sealed class JobLoggingFilter : JobFilterAttribute, IServerFilter
{
    private const string StopwatchKey = "JobLoggingFilter.Stopwatch";
    private const string LogContextTokenKey = "JobLoggingFilter.LogContextToken";

    public void OnPerforming(PerformingContext filterContext)
    {
        var jobName = BuildJobName(filterContext.BackgroundJob.Job);

        filterContext.Items[StopwatchKey] = Stopwatch.StartNew();
        filterContext.Items[LogContextTokenKey] = LogContext.PushProperty("JobName", jobName);

        Log.Information("Starting Hangfire job {JobName} ({JobId}).", jobName, filterContext.BackgroundJob.Id);
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        var jobName = BuildJobName(filterContext.BackgroundJob.Job);
        var durationMs = filterContext.Items.TryGetValue(StopwatchKey, out var rawStopwatch) && rawStopwatch is Stopwatch stopwatch
            ? stopwatch.Elapsed.TotalMilliseconds
            : (double?)null;

        if (filterContext.Exception is not null && !filterContext.ExceptionHandled)
        {
            Log.Error(
                filterContext.Exception,
                "Hangfire job {JobName} ({JobId}) failed after {DurationMs} ms.",
                jobName,
                filterContext.BackgroundJob.Id,
                durationMs);
        }
        else
        {
            Log.Information(
                "Hangfire job {JobName} ({JobId}) completed in {DurationMs} ms.",
                jobName,
                filterContext.BackgroundJob.Id,
                durationMs);
        }

        if (filterContext.Items.TryGetValue(LogContextTokenKey, out var rawToken) && rawToken is IDisposable token)
        {
            token.Dispose();
        }
    }

    private static string BuildJobName(Job job) => $"{job.Type.Name}.{job.Method.Name}";
}

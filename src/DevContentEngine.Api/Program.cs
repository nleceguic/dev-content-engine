using DevContentEngine.Api;
using DevContentEngine.Application;
using DevContentEngine.Application.UseCases.GenerationRuns.GetRecentGenerationRuns;
using DevContentEngine.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInfrastructureHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

app.MapGet("/generation-runs", async (int? count, ISender sender, CancellationToken cancellationToken) =>
{
    var effectiveCount = count is > 0 ? count.Value : 20;
    var runs = await sender.Send(new GetRecentGenerationRunsQuery(effectiveCount), cancellationToken);

    return Results.Ok(runs);
});

app.Run();

using System.Diagnostics;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Investigation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (structured logging)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console();
});

// ActivitySource used by application for spans
var activitySourceName = "InvestigationService";
builder.Services.AddSingleton(new ActivitySource(activitySourceName));

// OpenTelemetry tracing configuration
builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
{
    var svcName = builder.Environment.ApplicationName ?? "InvestigationService";
    tracerProviderBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(svcName))
        .AddSource(activitySourceName)
        .AddAspNetCoreInstrumentation(options =>
        {
            // Enrich activities with request headers if needed
            options.Enrich = (activity, eventName, obj) => { /* lightweight no-op for now */ };
        })
        .AddHttpClientInstrumentation();

    // Console exporter for local debugging; switch to OTLP in production via config
    tracerProviderBuilder.AddConsoleExporter();

    var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        tracerProviderBuilder.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(otlpEndpoint);
        });
    }
});

builder.Services.AddControllers();

// MediatR
builder.Services.AddMediatR(typeof(Investigation.Application.Commands.RunInvestigationCommand).Assembly);

// Application services
builder.Services.AddScoped<Investigation.Application.Services.IInvestigationOrchestratorService, Investigation.Application.Services.InvestigationOrchestratorService>();

// Infrastructure (HttpClients, policies, session repo)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware: extract or create trace id, start Activity and push TraceId into Serilog LogContext
app.Use(async (context, next) =>
{
    var activitySource = context.RequestServices.GetRequiredService<ActivitySource>();

    var traceId = context.Request.Headers.ContainsKey("X-Trace-Id")
        ? context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? string.Empty
        : Activity.Current?.Id ?? Activity.NewId().ToString();

    using (LogContext.PushProperty("TraceId", traceId))
    using (var activity = activitySource.StartActivity("IncomingRequest", ActivityKind.Server))
    {
        if (activity is not null)
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.AddTag("traceId", traceId);
        }

        // Ensure downstream handlers can read the trace id header
        if (!context.Request.Headers.ContainsKey("X-Trace-Id"))
        {
            context.Request.Headers.Add("X-Trace-Id", traceId);
        }

        await next();

        activity?.Stop();
    }
});

app.MapControllers();

app.Run();

using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Context;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Investigation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console();
});

// ActivitySource
var activitySourceName = "InvestigationService";
builder.Services.AddSingleton(new ActivitySource(activitySourceName));

// OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        var svcName = builder.Environment.ApplicationName ?? "InvestigationService";

        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(svcName))
            .AddSource(activitySourceName)
            .AddAspNetCoreInstrumentation() // drop Enrich for now
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracerProviderBuilder.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otlpEndpoint);
            });
        }
    });

// Controllers
builder.Services.AddControllers();

// MediatR
builder.Services.AddMediatR(typeof(Investigation.Application.Commands.RunInvestigationCommand).Assembly);

// Application services
builder.Services.AddScoped<Investigation.Application.Services.IInvestigationOrchestratorService,
    Investigation.Application.Services.InvestigationOrchestratorService>();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware: TraceId
app.Use(async (context, next) =>
{
    var activitySource = context.RequestServices.GetRequiredService<ActivitySource>();

    var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault()
                  ?? Activity.Current?.TraceId.ToString()
                  ?? Guid.NewGuid().ToString();

    using (LogContext.PushProperty("TraceId", traceId))
    using (var activity = activitySource.StartActivity("IncomingRequest", ActivityKind.Server))
    {
        if (activity != null)
        {
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.AddTag("traceId", traceId);
        }

        context.Request.Headers["X-Trace-Id"] = traceId;

        await next();

        activity?.Stop();
    }
});

app.MapControllers();
app.Run();
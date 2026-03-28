using System;
using Investigation.Application.Contracts;
using Investigation.Application.Orchestration;
using Investigation.Application.Services;
using Investigation.Infrastructure.Clients;
using Investigation.Infrastructure.Policies;
using Investigation.Infrastructure.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;

namespace Investigation.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration config)
        {
            // Session
            services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

            // Orchestrator
            services.AddScoped<IInvestigationOrchestrator, InvestigationOrchestrator>();

            // Plan validator
            services.AddScoped<PlanValidator>();

            // Base URLs from config
            var aiBase = config["ExternalServices:AiAgent:BaseUrl"] ?? "http://ai-agent:8501";
            var toolBase = config["ExternalServices:ToolExecution:BaseUrl"] ?? "http://tool-exec:8080";

            // AI Agent client — handles both /plan and /analyze
            services.AddHttpClient<AiAgentClient>(client =>
            {
                client.BaseAddress = new Uri(aiBase.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
            }).AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            services.AddScoped<IAiPlanClient>(sp =>
                sp.GetRequiredService<AiAgentClient>());
            services.AddScoped<IAiAgentClient>(sp =>
                sp.GetRequiredService<AiAgentClient>());

            // Tool Execution client
            services.AddHttpClient<IToolExecutionClient, ToolExecutionClient>(client =>
            {
                client.BaseAddress = new Uri(toolBase.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(90);
            }).AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            return services;
        }
    }
}

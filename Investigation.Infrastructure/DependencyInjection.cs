using System;
using Investigation.Application.Contracts;
using Investigation.Application.Orchestration;
using Investigation.Application.Services;
using Investigation.Infrastructure.Clients;
using Investigation.Infrastructure.Policies;
using Investigation.Infrastructure.Session;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using System.Net.Http;

namespace Investigation.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

            // Register orchestrator
            services.AddScoped<IInvestigationOrchestrator, InvestigationOrchestrator>();

            // Register new services for plan integration
            services.AddScoped<PlanValidator>();

            // Use mock AI client for development
            services.AddScoped<IAiAgentClient, MockAiAgentClient>();

            var aiBase = config["AiAgent:BaseUrl"] ?? config["ExternalServices:AiAgent:BaseUrl"] ?? "http://ai-agent";
            var ragBase = config["ExternalServices:Rag:BaseUrl"] ?? "http://rag-service";
            var toolBase = config["ExternalServices:ToolExecution:BaseUrl"] ?? "http://tool-exec";

            // Typed HttpClient for AI Agent planning (/plan endpoint)
            services.AddHttpClient<IAiPlanClient, Investigation.Application.Services.AiAgentClient>(client =>
            {
                client.BaseAddress = new Uri(aiBase);

                // 60s timeout — LLM planning calls can be slow
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            // Legacy clients (for backward compatibility)
            services.AddHttpClient<IAgentClient, Clients.AiAgentClient>(c => c.BaseAddress = new Uri(aiBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            services.AddHttpClient<IRagClient, RagClient>(c => c.BaseAddress = new Uri(ragBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            services.AddHttpClient<IToolExecutionClient, ToolExecutionClient>(c => c.BaseAddress = new Uri(toolBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            return services;
        }
    }
}

using System;
using Investigation.Application.Contracts;
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

            var aiBase = config["ExternalServices:AiAgent:BaseUrl"] ?? "http://ai-agent";
            var ragBase = config["ExternalServices:Rag:BaseUrl"] ?? "http://rag-service";
            var toolBase = config["ExternalServices:ToolExecution:BaseUrl"] ?? "http://tool-exec";

            services.AddHttpClient<IAgentClient, AiAgentClient>(c => c.BaseAddress = new Uri(aiBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            services.AddHttpClient<IRagClient, RagClient>(c => c.BaseAddress = new Uri(ragBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            services.AddHttpClient<IToolExecutionClient, ToolExecutionClient>(c => c.BaseAddress = new Uri(toolBase))
                .AddPolicyHandler(PolicyFactory.GetRetryPolicy());

            return services;
        }
    }
}

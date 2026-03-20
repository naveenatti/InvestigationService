using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.Models;
using Investigation.Domain;
using Microsoft.Extensions.Logging;

namespace Investigation.Infrastructure.Clients
{
    public class AiAgentClient : IAiPlanClient, IAiAgentClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<AiAgentClient> _logger;
        private readonly ActivitySource _activitySource;

        public AiAgentClient(
            HttpClient http,
            ILogger<AiAgentClient> logger,
            ActivitySource activitySource)
        {
            _http = http;
            _logger = logger;
            _activitySource = activitySource;
        }

        public async Task<PlanResponse> GetPlanAsync(
            string userQuery,
            List<ToolDefinitionDto> toolRegistry,
            List<string> namespaces,
            string traceId,
            CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity(
                "AiAgentClient.GetPlan", ActivityKind.Client);
            activity?.AddTag("traceId", traceId);

            var payload = new
            {
                userQuery,
                toolRegistry = toolRegistry.Select(t => new
                {
                    name = t.Name,
                    description = t.Description,
                    parameters = t.Parameters?.ToDictionary(
                        p => p.Key,
                        p => new
                        {
                            type = p.Value.Type,
                            required = p.Value.Required,
                            description = p.Value.Description,
                            @default = p.Value.Default
                        })
                }),
                context = new
                {
                    availableNamespaces = namespaces
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "plan")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.TryAddWithoutValidation("x-correlation-id", traceId);

            _logger.LogDebug(
                "Calling /plan. query={Query} tools={ToolCount} namespaces={Namespaces} traceId={TraceId}",
                userQuery,
                toolRegistry.Count,
                string.Join(",", namespaces),
                traceId);

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();

            var plan = await resp.Content.ReadFromJsonAsync<PlanResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct) ?? new PlanResponse();

            _logger.LogInformation(
                "Plan received. intent={Intent} steps={Steps} traceId={TraceId}",
                plan.SummaryIntent,
                plan.InvestigationPlan.Count,
                traceId);

            return plan;
        }

        public Task<AgentResponse> InvestigateAsync(
            string query,
            string caseId,
            string traceId,
            CancellationToken ct = default)
        {
            // The new orchestration layer owns planning + tool execution.
            // This method is kept to satisfy the existing interface contract.
            return AnalyzeAsync(query, new List<ToolResult>(), traceId, ct);
        }

        public async Task<AgentResponse> AnalyzeAsync(
            string query,
            List<ToolResult> toolResults,
            string traceId,
            CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity(
                "AiAgentClient.Analyze", ActivityKind.Client);
            activity?.AddTag("traceId", traceId);

            var payload = new
            {
                userQuery = query,
                toolResults = toolResults.Select(r => new
                {
                    toolName = r.ToolName,
                    status = r.Success ? "success" : "failed",
                    inputArguments = (object?)null,   // placeholder — tool caller owned
                    output = r.Result,
                    errorMessage = (string?)null,
                    durationMs = 0
                })
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "analyze")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.TryAddWithoutValidation("x-correlation-id", traceId);

            _logger.LogDebug(
                "Calling /analyze. results={Count} traceId={TraceId}",
                toolResults.Count,
                traceId);

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();

            var analysis = await resp.Content.ReadFromJsonAsync<AnalysisResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            return new AgentResponse
            {
                ReasoningSummary = analysis?.RootCause ?? "Analysis unavailable"
            };
        }
    }

    /// <summary>
    /// Maps the /analyze response from the AI Agent.
    /// Internal to this client.
    /// </summary>
    internal class AnalysisResponse
    {
        public string RootCause { get; set; } = string.Empty;
        public List<string> Evidence { get; set; } = new();
        public double Confidence { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public List<string> ToolsUsed { get; set; } = new();
    }
}

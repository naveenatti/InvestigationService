using System.Net.Http.Json;
using System.Text.Json;
using Investigation.Application.Exceptions;
using Investigation.Application.Models;
using Microsoft.Extensions.Logging;

using Investigation.Application.Contracts;

namespace Investigation.Application.Services
{
    public class AiAgentClient : IAiPlanClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<AiAgentClient> _logger;

        public AiAgentClient(HttpClient http, ILogger<AiAgentClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<PlanResponse> GetPlanAsync(
            string userQuery,
            List<string> toolRegistry,
            string traceId,
            CancellationToken ct = default)
        {
            // Build request body matching agent PlanningRequest schema exactly
            var body = new
            {
                userQuery    = userQuery,     // camelCase — matches agent schema
                toolRegistry = toolRegistry   // camelCase — matches agent schema
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/plan")
            {
                Content = JsonContent.Create(body)
            };

            // Pass traceId as correlation header — agent accepts x-correlation-id
            request.Headers.TryAddWithoutValidation("x-correlation-id", traceId);

            _logger.LogInformation(
                "Calling POST /plan. userQuery={Query} toolCount={Count} traceId={TraceId}",
                userQuery, toolRegistry.Count, traceId);

            var response = await _http.SendAsync(request, ct);

            // Handle 422 separately — it means the agent rejected the request shape
            if ((int)response.StatusCode == 422)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidPlanException(
                    $"Agent returned 422 validation error: {detail}");
            }

            response.EnsureSuccessStatusCode();

            // PropertyNameCaseInsensitive: agent returns camelCase, C# models are PascalCase
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var plan = await response.Content
                .ReadFromJsonAsync<PlanResponse>(options, ct)
                ?? throw new InvalidOperationException("Agent returned null plan.");

            _logger.LogInformation(
                "Plan received. planId={PlanId} intent={Intent} steps={Steps} traceId={TraceId}",
                plan.PlanId, plan.SummaryIntent, plan.InvestigationPlan.Count, traceId);

            return plan;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.Models;
using Microsoft.Extensions.Logging;

namespace Investigation.Infrastructure.Clients
{
    public class ToolExecutionClient : IToolExecutionClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ToolExecutionClient> _logger;
        private readonly ActivitySource _activitySource;

        public ToolExecutionClient(
            HttpClient http,
            ILogger<ToolExecutionClient> logger,
            ActivitySource activitySource)
        {
            _http = http;
            _logger = logger;
            _activitySource = activitySource;
        }

        /// <summary>
        /// GET /api/engine/tools
        /// Fetches all registered enabled tools with structured parameter definitions.
        /// </summary>
        public async Task<List<ToolDefinitionDto>> GetToolsAsync(CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity(
                "ToolExecutionClient.GetTools", ActivityKind.Client);
            activity?.AddTag("http.url", $"{_http.BaseAddress}api/engine/tools");

            var response = await _http.GetFromJsonAsync<ListToolsResponse>(
                "api/engine/tools", ct);

            var tools = response?.Tools ?? new List<ToolDefinitionDto>();
            activity?.AddTag("tool.count", tools.Count.ToString());

            _logger.LogDebug("Fetched {Count} tools from Tool Execution Service", tools.Count);
            return tools;
        }

        /// <summary>
        /// POST /api/engine/tools/list-namespaces/execute
        /// Fetches all available Kubernetes namespaces.
        /// Called in parallel with GetToolsAsync before planning.
        /// </summary>
        public async Task<List<string>> GetNamespacesAsync(
            string traceId, CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity(
                "ToolExecutionClient.GetNamespaces", ActivityKind.Client);
            activity?.AddTag("traceId", traceId);

            var payload = new
            {
                input = new { },
                traceId,
                correlationId = traceId
            };

            var resp = await _http.PostAsJsonAsync(
                "api/engine/tools/list-namespaces/execute", payload, ct);
            resp.EnsureSuccessStatusCode();

            var result = await resp.Content
                .ReadFromJsonAsync<ExecuteToolResponse>(ct);

            // Defensive parsing: Output["namespaces"] may be null or not an array.
            var namespacesArray = result?.Output?["namespaces"]?.AsArray();
            var namespaces = namespacesArray?
                .Select(n => n?.GetValue<string>() ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList()
                ?? new List<string>();

            activity?.AddTag("namespace.count", namespaces.Count.ToString());
            _logger.LogDebug(
                "Fetched {Count} namespaces: {Namespaces}",
                namespaces.Count,
                string.Join(", ", namespaces));

            return namespaces;
        }

        /// <summary>
        /// POST /api/engine/tools/{toolName}/execute
        /// Executes a single tool with the given arguments.
        /// toolName is in the path — not the request body.
        /// </summary>
        public async Task<JsonObject> ExecuteToolAsync(
            string toolName,
            JsonObject? arguments,
            string traceId,
            CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity(
                "ToolExecutionClient.ExecuteTool", ActivityKind.Client);
            activity?.AddTag("traceId", traceId);
            activity?.AddTag("tool.name", toolName);

            var payload = new
            {
                input = arguments ?? new JsonObject(),
                traceId,
                correlationId = traceId
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/engine/tools/{toolName}/execute")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Trace-Id", traceId);

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content
                .ReadFromJsonAsync<JsonObject>(ct) ?? new JsonObject();

            activity?.SetTag("http.status_code", ((int)resp.StatusCode).ToString());
            return json;
        }
    }
}
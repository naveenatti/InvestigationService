using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Microsoft.Extensions.Logging;
using Polly;
using System.Diagnostics;

namespace Investigation.Infrastructure.Clients
{
    public class RagClient : IRagClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<RagClient> _logger;
        private readonly ActivitySource _activitySource;

        public RagClient(HttpClient http, ILogger<RagClient> logger, ActivitySource activitySource)
        {
            _http = http;
            _logger = logger;
            _activitySource = activitySource;
        }

        public async Task<JsonObject> CallRagAsync(string queryHints, string traceId, CancellationToken ct = default)
        {
            using var activity = _activitySource.StartActivity("RagClient.CallRag", ActivityKind.Client);
            if (activity is not null)
            {
                activity.SetIdFormat(ActivityIdFormat.W3C);
                activity.AddTag("traceId", traceId);
                activity.AddTag("http.method", "POST");
                activity.AddTag("http.url", _http.BaseAddress?.ToString()?.TrimEnd('/') + "/rag/search");
            }

            var payload = new JsonObject
            {
                ["queryHints"] = queryHints,
                ["traceId"] = traceId
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "rag/search")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Trace-Id", traceId);

            var resp = await _http.SendAsync(request, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: ct) ?? new JsonObject();

            activity?.SetTag("http.status_code", ((int)resp.StatusCode).ToString());
            return json;
        }
    }
}

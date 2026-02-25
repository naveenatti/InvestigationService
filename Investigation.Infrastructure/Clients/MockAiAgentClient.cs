using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Domain;
using Microsoft.Extensions.Logging;

namespace Investigation.Infrastructure.Clients
{
    /// <summary>
    /// Mock AI agent returning hardcoded response for development.
    /// </summary>
    public class MockAiAgentClient : IAiAgentClient
    {
        private readonly ILogger<MockAiAgentClient> _logger;

        public MockAiAgentClient(ILogger<MockAiAgentClient> logger)
        {
            _logger = logger;
        }

        public async Task<AgentResponse> InvestigateAsync(string query, string caseId, string traceId, CancellationToken ct = default)
        {
            _logger.LogInformation("Mock AI Agent called for query: {Query}, caseId: {CaseId}, traceId: {TraceId}", query, caseId, traceId);

            // Simulate some async work
            await Task.Delay(50, ct);

            return new AgentResponse
            {
                Reasoning = $"Analyzed query '{query}' for case '{caseId}'. Recommending search_documents action.",
                ReasoningSummary = $"Mock reasoning for: {query}",
                Actions = new List<AgentAction>
                {
                    new AgentAction { ToolName = "search_documents", Input = new { query = query, limit = 5 } }
                }
            };
        }
    }
}

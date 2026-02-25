using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Domain;

namespace Investigation.Application.Services
{
    /// <summary>
    /// DEPRECATED: Use Investigation.Application.Orchestration.InvestigationOrchestrator instead.
    /// Keep for backward compatibility with MediatR command handler.
    /// </summary>
    public class InvestigationOrchestratorService : IInvestigationOrchestratorService
    {
        private readonly IAgentClient _agentClient;
        private readonly IRagClient _ragClient;
        private readonly IToolExecutionClient _toolClient;
        private readonly ISessionRepository _sessionRepository;
        private readonly ActivitySource _activitySource;

        public InvestigationOrchestratorService(
            IAgentClient agentClient,
            IRagClient ragClient,
            IToolExecutionClient toolClient,
            ISessionRepository sessionRepository,
            ActivitySource activitySource)
        {
            _agentClient = agentClient;
            _ragClient = ragClient;
            _toolClient = toolClient;
            _sessionRepository = sessionRepository;
            _activitySource = activitySource;
        }

        public async Task<InvestigationResultDto> RunInvestigationAsync(string query, string? sessionId, string traceId, CancellationToken ct = default)
        {
            var sessionGuid = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid() : Guid.Parse(sessionId);

            // Load or create session context
            var session = await _sessionRepository.GetAsync(sessionGuid, ct) ?? new InvestigationSession(sessionGuid, null);

            var toolCalls = new List<ToolCallDto>();

            // PLACEHOLDER: Old orchestration flow kept for backward compat
            // New code should use Investigation.Application.Orchestration.InvestigationOrchestrator

            var summary = "Legacy orchestrator called";
            return new InvestigationResultDto(session.Id.ToString(), traceId, summary, null, toolCalls, 0);
        }
    }
}

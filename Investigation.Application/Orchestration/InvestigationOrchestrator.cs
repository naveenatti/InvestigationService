using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;
using Investigation.Domain;
using Microsoft.Extensions.Logging;

namespace Investigation.Application.Orchestration
{
    /// <summary>
    /// Orchestrates investigation queries following Clean Architecture principles.
    /// Validates requests, calls AI agent, maps results to tool calls, and returns typed responses.
    /// Emits OpenTelemetry traces and structured logs for observability.
    /// </summary>
    public class InvestigationOrchestrator : IInvestigationOrchestrator
    {
        private readonly IAiAgentClient _aiClient;
        private readonly ISessionRepository _sessionRepository;
        private readonly ActivitySource _activitySource;
        private readonly ILogger<InvestigationOrchestrator> _logger;

        public InvestigationOrchestrator(
            IAiAgentClient aiClient,
            ISessionRepository sessionRepository,
            ActivitySource activitySource,
            ILogger<InvestigationOrchestrator> logger)
        {
            _aiClient = aiClient;
            _sessionRepository = sessionRepository;
            _activitySource = activitySource;
            _logger = logger;
        }

        /// <summary>
        /// Executes investigation orchestration flow with validation, AI agent invocation, and response mapping.
        /// </summary>
        public async Task<InvestigationResponse> InvestigateAsync(InvestigationRequest request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            // Step 1: Start Activity (OpenTelemetry)
            using var activity = _activitySource.StartActivity("investigation.orchestrate", ActivityKind.Internal);
            activity?.SetIdFormat(ActivityIdFormat.W3C);
            activity?.AddTag("traceId", request.TraceId);
            activity?.AddTag("caseId", request.CaseId);

            try
            {
                _logger.LogInformation("Investigation started for CaseId={CaseId}, Query={Query}", request.CaseId, request.Query);

                // Step 2: Validate request
                ValidateRequest(request);

                var sessionGuid = Guid.NewGuid();
                var session = new InvestigationSession(
                    sessionGuid,
                    request.UserId,
                    request.TraceId,
                    request.CaseId,
                    request.Query);

                // Step 3: Call IAiAgentClient
                _logger.LogInformation("Calling AI Agent for TraceId={TraceId}", request.TraceId);

                AgentResponse agentResp;
                using (var aiActivity = _activitySource.StartActivity("ai.agent.called", ActivityKind.Client))
                {
                    aiActivity?.SetIdFormat(ActivityIdFormat.W3C);
                    aiActivity?.AddTag("traceId", request.TraceId);
                    aiActivity?.AddTag("caseId", request.CaseId);

                    agentResp = await _aiClient.InvestigateAsync(
                        request.Query,
                        request.CaseId,
                        request.TraceId ?? string.Empty,
                        ct);
                }

                _logger.LogInformation("AI Agent returned {ActionCount} actions", agentResp?.Actions?.Count ?? 0);

                // Step 4 & 5: Map AI result → InvestigationResponse + Add ToolCalls
                var toolCalls = MapActionsToToolCalls(agentResp?.Actions);

                sw.Stop();

                // Step 6 & 7: Capture duration and return response
                var response = new InvestigationResponse(
                    request.TraceId ?? string.Empty,
                    request.CaseId,
                    InvestigationResponseStatus.Success,
                    agentResp?.ReasoningSummary ?? agentResp?.Reasoning ?? "Analysis completed",
                    agentResp != null ? new { insight = "Investigation performed" } : null,
                    toolCalls,
                    sw.ElapsedMilliseconds,
                    DateTime.UtcNow
                );

                // Persist session
                session.MarkCompleted();
                await _sessionRepository.SaveAsync(session, ct);

                _logger.LogInformation("Investigation completed for CaseId={CaseId} in {DurationMs}ms", request.CaseId, sw.ElapsedMilliseconds);

                activity?.AddTag("http.status_code", "200");
                activity?.AddTag("investigation.duration_ms", sw.ElapsedMilliseconds);

                return response;
            }
            catch (ArgumentException ex)
            {
                sw.Stop();
                _logger.LogWarning("Investigation validation failed: {Message}", ex.Message);
                activity?.AddTag("http.status_code", "400");
                activity?.AddTag("error", true);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Investigation failed for CaseId={CaseId}", request.CaseId);
                activity?.AddTag("http.status_code", "500");
                activity?.AddTag("error", true);
                activity?.AddTag("error.message", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Validates investigation request for required fields.
        /// </summary>
        private void ValidateRequest(InvestigationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null");

            if (string.IsNullOrWhiteSpace(request.Query))
                throw new ArgumentException("Query is required", nameof(request.Query));

            if (string.IsNullOrWhiteSpace(request.CaseId))
                throw new ArgumentException("CaseId is required", nameof(request.CaseId));

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required", nameof(request.UserId));
        }

        /// <summary>
        /// Maps agent actions to tool call DTOs for response contract.
        /// </summary>
        private List<Contracts.ToolCallDto> MapActionsToToolCalls(List<AgentAction>? actions)
        {
            var toolCalls = new List<Contracts.ToolCallDto>();

            if (actions == null || actions.Count == 0)
                return toolCalls;

            foreach (var action in actions)
            {
                toolCalls.Add(new Contracts.ToolCallDto(
                    action.ToolName,
                    "Pending",
                    0,
                    new { action = action.ToolName, input = action.Input }
                ));
            }

            return toolCalls;
        }
    }
}

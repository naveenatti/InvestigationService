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
    // Orchestrator implements the orchestration flow described in requirements.
    // It coordinates calls to the AI agent, RAG, and Tool Execution services,
    // persists session state, and aggregates results.
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

            var steps = new List<InvestigationStepDto>();

            // 1) Call AI Agent
            var stepAgent = new InvestigationStep("AI Agent");
            session.AddStep(stepAgent);
            var sw = Stopwatch.StartNew();
            try
            {
                using var activity = _activitySource.StartActivity("CallAgent", ActivityKind.Internal);
                if (activity is not null)
                {
                    activity.SetIdFormat(ActivityIdFormat.W3C);
                    activity.AddTag("traceId", traceId);
                }

                var agentResp = await _agentClient.CallAgentAsync(query, session.Id.ToString(), traceId, ct);

                activity?.Stop();
                sw.Stop();
                stepAgent.MarkSuccess(agentResp.ToString());

                steps.Add(new InvestigationStepDto("AI Agent", stepAgent.Status.ToString(), sw.ElapsedMilliseconds, agentResp));

                // Extract toolcalls from agent response
                var toolCalls = agentResp["toolCalls"]?.AsArray() ?? default;

                // 2) Call RAG for hints
                var stepRag = new InvestigationStep("RAG Service");
                session.AddStep(stepRag);
                var swRag = Stopwatch.StartNew();
                JsonObject? ragResp = null;
                try
                {
                    using var ragActivity = _activitySource.StartActivity("CallRag", ActivityKind.Internal);
                    if (ragActivity is not null)
                    {
                        ragActivity.SetIdFormat(ActivityIdFormat.W3C);
                        ragActivity.AddTag("traceId", traceId);
                    }

                    ragResp = await _ragClient.CallRagAsync(agentResp["reasoningSummary"]?.GetValue<string>() ?? query, traceId, ct);

                    ragActivity?.Stop();
                    swRag.Stop();
                    stepRag.MarkSuccess(ragResp?.ToString());
                    steps.Add(new InvestigationStepDto("RAG Service", stepRag.Status.ToString(), swRag.ElapsedMilliseconds, ragResp));
                }
                catch (Exception ex)
                {
                    swRag.Stop();
                    stepRag.MarkFailed(ex.Message);
                    steps.Add(new InvestigationStepDto("RAG Service", stepRag.Status.ToString(), swRag.ElapsedMilliseconds, JsonObject.Parse($"{new { error = ex.Message }.ToString() ?? "{}"}")));
                }

                // 3) Execute Tools
                if (toolCalls is not null)
                {
                    foreach (var tc in toolCalls)
                    {
                        var toolName = tc?["toolName"]?.GetValue<string>() ?? "unknown";
                        var args = tc?["arguments"] as JsonObject
                                    ?? tc?["arguments"]?.AsObject();

                        var stepTool = new InvestigationStep($"Tool:{toolName}");
                        session.AddStep(stepTool);
                        var swTool = Stopwatch.StartNew();
                        try
                        {
                            using var toolActivity = _activitySource.StartActivity($"CallTool-{toolName}", ActivityKind.Internal);
                            if (toolActivity is not null)
                            {
                                toolActivity.SetIdFormat(ActivityIdFormat.W3C);
                                toolActivity.AddTag("traceId", traceId);
                            }

                            var toolResult = await _toolClient.ExecuteToolAsync(toolName, args, traceId, ct);

                            toolActivity?.Stop();
                            swTool.Stop();
                            stepTool.MarkSuccess(toolResult.ToString());
                            steps.Add(new InvestigationStepDto($"Tool:{toolName}", stepTool.Status.ToString(), swTool.ElapsedMilliseconds, toolResult));
                        }
                        catch (Exception ex)
                        {
                            swTool.Stop();
                            stepTool.MarkFailed(ex.Message);
                            steps.Add(new InvestigationStepDto($"Tool:{toolName}", stepTool.Status.ToString(), swTool.ElapsedMilliseconds, JsonObject.Parse($"{new { error = ex.Message }.ToString() ?? "{}"}")));
                        }
                    }
                }

                // Persist session
                await _sessionRepository.SaveAsync(session, ct);

                // Aggregate result summary
                var summary = agentResp["reasoningSummary"]?.GetValue<string>() ?? "No summary";

                return new InvestigationResultDto(session.Id.ToString(), traceId, summary, steps);
            }
            catch (Exception ex)
            {
                sw.Stop();
                steps.Add(new InvestigationStepDto("AI Agent", InvestigationStepStatus.Failed.ToString(), sw.ElapsedMilliseconds, JsonObject.Parse($"{{ \"error\": \"{ex.Message}\" }}")));
                await _sessionRepository.SaveAsync(session, ct);
                throw;
            }
        }
    }
}

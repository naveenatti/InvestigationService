using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;
using Investigation.Application.Models;
using Investigation.Application.Services;
using Investigation.Domain;
using Microsoft.Extensions.Logging;

namespace Investigation.Application.Orchestration
{
    public class InvestigationOrchestrator : IInvestigationOrchestrator
    {
        private readonly IAiPlanClient _planClient;
        private readonly IAiAgentClient _analysisClient;
        private readonly IToolExecutionClient _toolClient;
        private readonly PlanValidator _validator;
        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<InvestigationOrchestrator> _logger;

        public InvestigationOrchestrator(
            IAiPlanClient planClient,
            IAiAgentClient analysisClient,
            IToolExecutionClient toolClient,
            PlanValidator validator,
            ISessionRepository sessionRepository,
            ILogger<InvestigationOrchestrator> logger)
        {
            _planClient     = planClient;
            _analysisClient = analysisClient;
            _toolClient     = toolClient;
            _validator      = validator;
            _sessionRepository = sessionRepository;
            _logger         = logger;
        }

        public async Task<InvestigationResponse> InvestigateAsync(
            InvestigationRequest req, CancellationToken ct = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.Query))
                throw new ArgumentException("Query is required", nameof(req.Query));

            var sw = Stopwatch.StartNew();

            var traceId = string.IsNullOrWhiteSpace(req.TraceId)
                ? Guid.NewGuid().ToString()
                : req.TraceId;

            _logger.LogInformation(
                "Investigation started. traceId={TraceId} caseId={CaseId} query={Query}",
                traceId, req.CaseId, req.Query);

            // ── Step 1: Pre-fetch tools and namespaces in parallel ─────────────
            // Both are needed before planning — run concurrently to save time.
            _logger.LogDebug("Pre-fetching tools and namespaces. traceId={TraceId}", traceId);

            var toolsTask      = _toolClient.GetToolsAsync(ct);
            var namespacesTask = _toolClient.GetNamespacesAsync(traceId, ct);
            await Task.WhenAll(toolsTask, namespacesTask);

            var tools      = toolsTask.Result;
            var namespaces = namespacesTask.Result;

            _logger.LogDebug(
                "Pre-fetch complete. tools={ToolCount} namespaces={Namespaces} traceId={TraceId}",
                tools.Count, string.Join(",", namespaces), traceId);

            // ── Step 2: Generate plan ─────────────────────────────────────────
            var plan = await _planClient.GetPlanAsync(
                userQuery:    req.Query!,
                toolRegistry: tools,
                namespaces:   namespaces,
                traceId:      traceId,
                ct:           ct);

            // ── Step 3: Validate plan ─────────────────────────────────────────
            _validator.Validate(plan);

            // ── Session persistence (restores prior session tracking) ─────────
            // Use TraceId as session Id when it's a valid Guid, otherwise fall back
            // to a new Guid. This keeps persistence aligned with ISessionRepository.
            var sessionId = Guid.TryParse(traceId, out var parsedTraceId)
                ? parsedTraceId
                : Guid.NewGuid();

            var session = new InvestigationSession(
                sessionId,
                req.UserId,
                traceId,
                req.CaseId,
                req.Query);

            _logger.LogInformation(
                "Plan validated. intent={Intent} steps={Steps} traceId={TraceId}",
                plan.SummaryIntent, plan.InvestigationPlan.Count, traceId);

            foreach (var step in plan.InvestigationPlan)
            {
                _logger.LogDebug(
                    "Step={Step} tool={Tool} params={Params} reasoning={Reasoning} traceId={TraceId}",
                    step.Step, step.ToolName,
                    JsonSerializer.Serialize(step.Parameters),
                    step.Reasoning, traceId);
            }

            // ── Step 4: Execute each plan step ────────────────────────────────
            var toolCalls   = new List<Investigation.Application.Contracts.ToolCallDto>();
            var toolResults = new List<ToolResult>();
            var status      = InvestigationResponseStatus.Success;

            foreach (var step in plan.InvestigationPlan)
            {
                toolCalls.Add(new Investigation.Application.Contracts.ToolCallDto(
                    step.ToolName, "Pending", 0,
                    new { action = step.ToolName, input = step.Parameters }));

                var sessionStep = new InvestigationStep(step.ToolName);
                session.AddStep(sessionStep);

                var stepSw = Stopwatch.StartNew();
                try
                {
                    var arguments = JsonSerializer.SerializeToNode(step.Parameters) as JsonObject;
                    var output    = await _toolClient.ExecuteToolAsync(
                        step.ToolName, arguments, traceId, ct);
                    stepSw.Stop();

                    toolResults.Add(new ToolResult(step.ToolName, true, output));
                    sessionStep.MarkSuccess(output?.ToJsonString());
                    toolCalls[^1] = toolCalls[^1] with
                    {
                        Status     = "Success",
                        DurationMs = stepSw.ElapsedMilliseconds,
                        Metadata   = new { action = step.ToolName, input = step.Parameters, output }
                    };
                }
                catch (Exception ex)
                {
                    stepSw.Stop();
                    status = InvestigationResponseStatus.Partial;
                    _logger.LogWarning(ex,
                        "Tool execution failed. tool={Tool} traceId={TraceId}",
                        step.ToolName, traceId);

                    toolResults.Add(new ToolResult(step.ToolName, false, null));
                    sessionStep.MarkFailed(ex.Message);
                    toolCalls[^1] = toolCalls[^1] with
                    {
                        Status     = "Failed",
                        DurationMs = stepSw.ElapsedMilliseconds,
                        Metadata   = new { action = step.ToolName, error = ex.Message }
                    };
                }
            }

            // ── Step 5: Analyze results ───────────────────────────────────────
            AgentResponse? analysis = null;
            try
            {
                analysis = await _analysisClient.AnalyzeAsync(
                    req.Query, toolResults, traceId, ct);
            }
            catch (Exception ex)
            {
                status = InvestigationResponseStatus.Partial;
                _logger.LogWarning(ex, "Analysis failed. traceId={TraceId}", traceId);
            }

            var summary = analysis?.ReasoningSummary ?? plan.SummaryIntent;
            var result  = analysis ?? new AgentResponse { ReasoningSummary = summary };

            // Keep session history aligned with response semantics:
            // - Success => Completed
            // - Partial => Completed (distinct from complete failure)
            // - Failed => Failed
            if (status == InvestigationResponseStatus.Failed)
                session.MarkFailed();
            else
                session.MarkCompleted();

            await _sessionRepository.SaveAsync(session, ct);

            // ── Step 6: Return response ───────────────────────────────────────
            return new InvestigationResponse(
                traceId,
                req.CaseId,
                status,
                summary,
                result,
                toolCalls,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow);
        }
    }
}
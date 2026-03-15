using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;
using Investigation.Application.Exceptions;
using Investigation.Application.Models;
using Investigation.Application.Services;
using Microsoft.Extensions.Logging;

namespace Investigation.Application.Orchestration
{
    /// <summary>
    /// Orchestrates investigation queries following Clean Architecture principles.
    /// Calls AI agent /plan endpoint, validates plan, and prepares for tool execution.
    /// </summary>
    public class InvestigationOrchestrator : IInvestigationOrchestrator
    {
        private readonly AiAgentClient _aiAgent;
        private readonly PlanValidator _validator;
        private readonly ILogger<InvestigationOrchestrator> _logger;

        // Registered tool names — sent as toolRegistry to the agent /plan endpoint
        // Must exactly match the allowlist in PlanValidator and routes in ToolExecution.API
        private static readonly List<string> ToolRegistry = new()
        {
            "list-pods",
            "get-pod-logs",
            "get-deployments",
            "get-resource-usage",
            "execute-command"
        };

        public InvestigationOrchestrator(
            AiAgentClient aiAgent,
            PlanValidator validator,
            ILogger<InvestigationOrchestrator> logger)
        {
            _aiAgent   = aiAgent;
            _validator = validator;
            _logger    = logger;
        }

        public async Task<InvestigationResponse> InvestigateAsync(InvestigationRequest req, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            // ── Step 1: Ensure traceId exists ─────────────────────────────────
            // InvestigationRequest.traceId is nullable — generate one if absent
            var traceId = string.IsNullOrWhiteSpace(req.TraceId)
                ? Guid.NewGuid().ToString()
                : req.TraceId;

            _logger.LogInformation(
                "Investigation started. traceId={TraceId} caseId={CaseId} query={Query}",
                traceId, req.CaseId, req.Query);

            // ── Step 2: Call POST /plan on the AI Agent ────────────────────────
            // userQuery comes from InvestigationRequest.query
            // toolRegistry is the fixed list of registered tools
            var plan = await _aiAgent.GetPlanAsync(
                userQuery:    req.Query!,
                toolRegistry: ToolRegistry,
                traceId:      traceId,
                ct:           ct);

            // ── Step 3: Validate the plan ──────────────────────────────────────
            // Throws InvalidPlanException if any tool is not on the allowlist
            // or if the plan structure is invalid
            _validator.Validate(plan);

            _logger.LogInformation(
                "Plan validated. planId={PlanId} intent={Intent} steps={Steps} traceId={TraceId}",
                plan.PlanId, plan.SummaryIntent, plan.InvestigationPlan.Count, traceId);

            // Log each step's reasoning — useful for debugging poor LLM plans
            foreach (var step in plan.InvestigationPlan)
            {
                _logger.LogDebug(
                    "Planned step={Step} tool={Tool} params={Params} reasoning={Reasoning} traceId={TraceId}",
                    step.Step, step.ToolName,
                    System.Text.Json.JsonSerializer.Serialize(step.Parameters),
                    step.Reasoning, traceId);
            }

            // ── TODO: Step 4 — Tool Execution Loop ────────────────────────────
            // For each step in plan.InvestigationPlan:
            //   call ToolExecutionClient.ExecuteAsync(step.ToolName, step.Parameters, traceId)
            //   collect results into List<ToolResultItem>
            // This is implemented in the next task.

            // ── TODO: Step 5 — POST /analyze ──────────────────────────────────
            // Call AiAgentClient.AnalyzeAsync(req.Query, toolResults, traceId)
            // Map diagnosis to InvestigationResponse.result and .summary
            // This is implemented in the next task.

            // ── Step 6: Return partial response (plan phase complete) ──────────
            return new InvestigationResponse(
                traceId,
                req.CaseId,
                InvestigationResponseStatus.Success,
                plan.SummaryIntent,  // temporary — replaced by /analyze later
                null,                 // populated after /analyze
                new List<Investigation.Application.Contracts.ToolCallDto>(), // populated after tool execution
                sw.ElapsedMilliseconds,
                DateTime.UtcNow
            );
        }
    }
}

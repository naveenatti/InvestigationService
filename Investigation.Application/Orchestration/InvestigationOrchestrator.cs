using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Investigation.Application.Contracts;
using Investigation.Application.DTOs;
using Investigation.Application.Exceptions;
using Investigation.Application.Models;
using Investigation.Application.Services;
using Investigation.Domain;
using Microsoft.Extensions.Logging;

namespace Investigation.Application.Orchestration
{
    /// <summary>
    /// Orchestrates investigation queries following Clean Architecture principles.
    /// Calls AI agent /plan endpoint, validates plan, executes tools, and summarizes.
    /// </summary>
    public class InvestigationOrchestrator : IInvestigationOrchestrator
    {
        private readonly IAiPlanClient _planClient;
        private readonly IAiAgentClient _analysisClient;
        private readonly IToolExecutionClient _toolClient;
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
            IAiPlanClient planClient,
            IAiAgentClient analysisClient,
            IToolExecutionClient toolClient,
            PlanValidator validator,
            ILogger<InvestigationOrchestrator> logger)
        {
            _planClient     = planClient;
            _analysisClient = analysisClient;
            _toolClient     = toolClient;
            _validator      = validator;
            _logger         = logger;
        }

        public async Task<InvestigationResponse> InvestigateAsync(InvestigationRequest req, CancellationToken ct = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrWhiteSpace(req.Query)) throw new ArgumentException("Query is required", nameof(req.Query));

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
            var plan = await _planClient.GetPlanAsync(
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

            // ── Step 4: Tool Execution Loop ───────────────────────────────
            // Execute each planned tool and collect the results for analysis.
            var toolCalls = new List<Investigation.Application.Contracts.ToolCallDto>();
            var toolResults = new List<ToolResult>();
            var status = InvestigationResponseStatus.Success;

            foreach (var step in plan.InvestigationPlan)
            {
                toolCalls.Add(new Investigation.Application.Contracts.ToolCallDto(
                    step.ToolName,
                    "Pending",
                    0,
                    new { action = step.ToolName, input = step.Parameters }));

                var stepSw = Stopwatch.StartNew();
                try
                {
                    var arguments = JsonSerializer.SerializeToNode(step.Parameters) as JsonObject;

                    var toolResultJson = await _toolClient.ExecuteToolAsync(step.ToolName, arguments, traceId, ct);

                    stepSw.Stop();
                    toolResults.Add(new ToolResult(step.ToolName, true, toolResultJson));

                    toolCalls[^1] = toolCalls[^1] with
                    {
                        Status = "Success",
                        DurationMs = stepSw.ElapsedMilliseconds,
                        Metadata = new { action = step.ToolName, input = step.Parameters, output = toolResultJson }
                    };
                }
                catch (Exception ex)
                {
                    stepSw.Stop();
                    status = InvestigationResponseStatus.Partial;
                    _logger.LogWarning(ex, "Tool execution failed. tool={ToolName} traceId={TraceId}", step.ToolName, traceId);

                    toolResults.Add(new ToolResult(step.ToolName, false, null));

                    toolCalls[^1] = toolCalls[^1] with
                    {
                        Status = "Failed",
                        DurationMs = stepSw.ElapsedMilliseconds,
                        Metadata = new { action = step.ToolName, input = step.Parameters, error = ex.Message }
                    };
                }
            }

            // ── Step 5: Analyze (LLM / AI) ────────────────────────────────
            // Provide the agent with the original query plus all tool outputs.
            AgentResponse? analysis = null;
            try
            {
                analysis = await _analysisClient.AnalyzeAsync(req.Query, toolResults, traceId, ct);
            }
            catch (Exception ex)
            {
                status = InvestigationResponseStatus.Partial;
                _logger.LogWarning(ex, "Analysis failed. traceId={TraceId}", traceId);
            }

            var summary = analysis?.ReasoningSummary ?? plan.SummaryIntent;
            var result = analysis ?? new AgentResponse { ReasoningSummary = summary };

            // ── Step 6: Return response ───────────────────────────────────
            return new InvestigationResponse(
                traceId,
                req.CaseId,
                status,
                summary,
                result,
                toolCalls,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow
            );
        }
    }
}

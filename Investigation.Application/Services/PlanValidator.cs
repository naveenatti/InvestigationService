using Investigation.Application.Exceptions;
using Investigation.Application.Models;
using Microsoft.Extensions.Logging;

namespace Investigation.Application.Services
{
    public class PlanValidator
    {
        // Exact tool names registered in ToolExecution.API — derived from OpenAPI paths
        private static readonly HashSet<string> AllowedTools = new()
        {
            "list-pods",
            "get-pod-logs",
            "get-deployments",
            "get-resource-usage",
            "execute-command"
        };

        private readonly ILogger<PlanValidator> _logger;

        public PlanValidator(ILogger<PlanValidator> logger)
        {
            _logger = logger;
        }

        public void Validate(PlanResponse plan)
        {
            // Must have at least one step
            if (!plan.InvestigationPlan.Any())
                throw new InvalidPlanException(
                    "Plan returned no steps.");

            // Cap at 10 steps — LLM should never plan more than this
            if (plan.InvestigationPlan.Count > 10)
                throw new InvalidPlanException(
                    $"Plan has {plan.InvestigationPlan.Count} steps — exceeds limit of 10.");

            foreach (var step in plan.InvestigationPlan)
            {
                // SECURITY: reject any tool not on the registered allowlist
                if (!AllowedTools.Contains(step.ToolName))
                    throw new InvalidPlanException(
                        $"Step {step.Step}: tool '{step.ToolName}' is not registered.");

                // Parameters null means the model returned a malformed step
                if (step.Parameters == null)
                    throw new InvalidPlanException(
                        $"Step {step.Step} ({step.ToolName}): parameters is null. " +
                        "Parameters is null.");

                // Warn on empty parameters — tool may work but results will be poor
                // get-pod-logs and get-resource-usage need podName to be useful
                if (!step.Parameters.Any())
                    _logger.LogWarning(
                        "Step {Step} tool={Tool} has empty parameters. " +
                        "Results may be incomplete.",
                        step.Step, step.ToolName);
            }
        }
    }
}
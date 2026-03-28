using System.Collections.Generic;

namespace Investigation.Application.Models
{
    /// <summary>
    /// Maps the /plan response from the AI Agent.
    /// planId removed — traceId correlation is owned by the Orchestration layer.
    /// </summary>
    public class PlanResponse
    {
        public string SummaryIntent { get; set; } = string.Empty;
        public List<PlanStep> InvestigationPlan { get; set; } = new();
    }

    public class PlanStep
    {
        public int Step { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? Reasoning { get; set; }
    }
}
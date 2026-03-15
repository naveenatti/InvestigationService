namespace Investigation.Application.Models
{
    public class PlanResponse
    {
        // Maps "planId" from agent response
        public string PlanId { get; set; } = string.Empty;

        // Maps "summaryIntent" from agent response
        public string SummaryIntent { get; set; } = string.Empty;

        // Maps "investigationPlan" array from agent response
        public List<PlanStep> InvestigationPlan { get; set; } = new();
    }

    public class PlanStep
    {
        public int Step { get; set; }
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? Reasoning { get; set; }   // optional — agent provides it, log it
    }
}
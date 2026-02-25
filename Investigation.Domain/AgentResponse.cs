using System.Collections.Generic;

namespace Investigation.Domain
{
    /// <summary>
    /// Response returned from AI agent clients.
    /// </summary>
    public class AgentResponse
    {
        public string? Reasoning { get; set; }
        public string? ReasoningSummary { get; set; }
        public List<AgentAction>? Actions { get; set; }
    }
}

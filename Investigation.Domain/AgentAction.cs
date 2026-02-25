namespace Investigation.Domain
{
    /// <summary>
    /// Represents an action the AI agent suggests (a tool call).
    /// </summary>
    public class AgentAction
    {
        public string ToolName { get; set; } = string.Empty;
        public object? Input { get; set; }
    }
}

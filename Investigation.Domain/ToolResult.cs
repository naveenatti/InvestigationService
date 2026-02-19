using System.Text.Json.Nodes;

namespace Investigation.Domain
{
    // Value object representing a tool execution result
    public record ToolResult(string ToolName, bool Success, JsonObject? Result);
}

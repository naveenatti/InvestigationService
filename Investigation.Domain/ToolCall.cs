using System.Text.Json.Nodes;

namespace Investigation.Domain
{
    // Value object describing a requested tool call
    public record ToolCall(string ToolName, JsonObject? Arguments);
}

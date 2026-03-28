using System.Collections.Generic;

namespace Investigation.Application.Models
{
    /// <summary>
    /// Mirrors ToolDefinitionDto from the Tool Execution Service.
    /// Used to pass structured tool metadata to the AI Agent planner.
    /// </summary>
    public class ToolDefinitionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsIdempotent { get; set; }
        public int TimeoutSeconds { get; set; }
        public Dictionary<string, ToolParameterDto>? Parameters { get; set; }
    }

    /// <summary>
    /// Mirrors ToolParameterDto from the Tool Execution Service.
    /// </summary>
    public class ToolParameterDto
    {
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = true;
        public string? Description { get; set; }
        public object? Default { get; set; }
    }

    /// <summary>
    /// Mirrors ListToolsResponse from the Tool Execution Service.
    /// Used to deserialize GET /api/engine/tools response.
    /// </summary>
    public class ListToolsResponse
    {
        public List<ToolDefinitionDto> Tools { get; set; } = new();
        public int Count { get; set; }
    }

    /// <summary>
    /// Mirrors ExecuteToolResponse from the Tool Execution Service.
    /// Used to deserialize POST /api/engine/tools/{toolName}/execute response.
    /// </summary>
    public class ExecuteToolResponse
    {
        public string? TraceId { get; set; }
        public string? CorrelationId { get; set; }
        public string? Status { get; set; }
        public bool Success { get; set; }
        public System.Text.Json.Nodes.JsonObject? Output { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public System.DateTime CompletedAt { get; set; }
    }
}
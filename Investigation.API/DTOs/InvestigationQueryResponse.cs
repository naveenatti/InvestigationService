using System.Collections.Generic;

namespace Investigation.API.DTOs
{
    /// <summary>
    /// Response model returned for investigation queries.
    /// </summary>
    public record InvestigationQueryResponse(
        string TraceId,
        string Status,
        string Summary,
        object? Result,
        List<ToolCallDto> ToolCalls,
        long DurationMs
    );

    public record ToolCallDto(
        string ToolName,
        object Input,
        object Output,
        string Status
    );
}

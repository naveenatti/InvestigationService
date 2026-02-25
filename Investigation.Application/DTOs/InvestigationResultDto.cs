using System.Collections.Generic;

namespace Investigation.Application
{
    // Kept for backward compatibility; new code should use Investigation.Application.DTOs.InvestigationResponse
    public record InvestigationResultDto(
        string TraceId,
        string Status,
        string Summary,
        object? Result,
        List<ToolCallDto> ToolCalls,
        long DurationMs
    );

    public record ToolCallDto(string ToolName, object Input, object Output, string Status);
}

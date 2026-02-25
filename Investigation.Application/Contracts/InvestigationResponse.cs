using System;
using System.Collections.Generic;

namespace Investigation.Application.Contracts
{
    /// <summary>
    /// Response contract returned for completed investigation queries.
    /// Contains summary, results, tool calls, and execution metadata.
    /// Supports OpenTelemetry trace propagation and structured logging.
    /// </summary>
    /// <remarks>
    /// Status values:
    /// - "Success": Investigation completed successfully with results.
    /// - "Failed": Investigation encountered an error during processing.
    /// - "Partial": Investigation completed but with limited results.
    /// </remarks>
    public record InvestigationResponse(
        /// <summary>Trace ID for correlating across distributed systems.</summary>
        string TraceId,

        /// <summary>Case identifier associated with this investigation.</summary>
        string CaseId,

        /// <summary>Status of investigation: Success, Failed, or Partial.</summary>
        string Status,

        /// <summary>Human-readable summary of the investigation findings.</summary>
        string Summary,

        /// <summary>Structured result data from AI analysis. Null if no results.</summary>
        object? Result,

        /// <summary>List of tool calls executed or planned during investigation.</summary>
        List<ToolCallDto> ToolCalls,

        /// <summary>Total duration of investigation in milliseconds.</summary>
        long DurationMs,

        /// <summary>UTC timestamp when investigation completed.</summary>
        DateTime TimestampUtc
    );

    /// <summary>
    /// Represents a single tool call executed or planned during investigation.
    /// </summary>
    /// <remarks>
    /// Status values:
    /// - "Pending": Tool call is queued for execution.
    /// - "Running": Tool call is currently executing.
    /// - "Success": Tool call completed successfully.
    /// - "Failed": Tool call encountered an error.
    /// </remarks>
    public record ToolCallDto(
        /// <summary>Name of the tool to invoke (e.g., "search_documents").</summary>
        string ToolName,

        /// <summary>Current status of the tool call.</summary>
        string Status,

        /// <summary>Execution duration in milliseconds (0 if pending).</summary>
        long DurationMs,

        /// <summary>Additional metadata including tool configuration and inputs.</summary>
        object? Metadata
    );
}

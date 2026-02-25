using System.Collections.Generic;

namespace Investigation.Application.DTOs
{
    /// <summary>
    /// Request model for investigation queries.
    /// Contains query text, case context, and user information for analysis.
    /// </summary>
    public record InvestigationRequest(
        /// <summary>Unique trace ID for correlating across services. Auto-generated if not provided.</summary>
        string? TraceId,

        /// <summary>Case identifier for grouping related investigations.</summary>
        string CaseId,

        /// <summary>Investigation query text (required). Example: "Why is pod restarting?"</summary>
        string Query,

        /// <summary>Optional context dictionary for additional request metadata.</summary>
        Dictionary<string, object>? Context,

        /// <summary>User ID or email. Required for auditing and access control.</summary>
        string UserId
    );
}

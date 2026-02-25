using System.Collections.Generic;

namespace Investigation.API.DTOs
{
    /// <summary>
    /// Request model for investigation queries.
    /// </summary>
    public record InvestigationQueryRequest(
        string? TraceId,
        string CaseId,
        string Query,
        Dictionary<string, object>? Context,
        string UserId
    );
}

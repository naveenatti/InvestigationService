using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Investigation.Application
{
    public record InvestigationResultDto
    (
        string SessionId,
        string TraceId,
        string Summary,
        IEnumerable<InvestigationStepDto> Steps
    );

    public record InvestigationStepDto(string Name, string Status, long DurationMs, JsonObject? Detail);
}

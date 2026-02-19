using MediatR;

namespace Investigation.Application.Commands
{
    // MediatR command dispatched by the API to run an investigation
    public record RunInvestigationCommand(string Query, string? SessionId, string TraceId) : IRequest<InvestigationResultDto>;
}

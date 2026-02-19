using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Investigation.Application.Services;

namespace Investigation.Application.Handlers
{
    public class RunInvestigationCommandHandler : IRequestHandler<Commands.RunInvestigationCommand, InvestigationResultDto>
    {
        private readonly IInvestigationOrchestratorService _orchestrator;

        public RunInvestigationCommandHandler(IInvestigationOrchestratorService orchestrator)
        {
            _orchestrator = orchestrator;
        }

        public async Task<InvestigationResultDto> Handle(Commands.RunInvestigationCommand request, CancellationToken cancellationToken)
        {
            // Start a local activity to get finer-grained tracing in the app layer
            using var activity = new Activity("RunInvestigationCommandHandler");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            var result = await _orchestrator.RunInvestigationAsync(request.Query, request.SessionId, request.TraceId, cancellationToken);

            activity.Stop();
            return result;
        }
    }
}

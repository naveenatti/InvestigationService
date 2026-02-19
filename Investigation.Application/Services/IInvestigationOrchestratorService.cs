using System.Threading;
using System.Threading.Tasks;

namespace Investigation.Application.Services
{
    public interface IInvestigationOrchestratorService
    {
        Task<InvestigationResultDto> RunInvestigationAsync(string query, string? sessionId, string traceId, CancellationToken ct = default);
    }
}
